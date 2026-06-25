using EatMyMovies.DataAccess.QueryModels;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TmdbMovie = TMDbLib.Objects.Movies.Movie;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMovies.Tests;

public class MovieServiceTests
{
    [Fact]
    public async Task BuildMovieList_PreservesRankingOrderAndReusesCachedApiResults()
    {
        var firstMovie = TestHelpers.CreateStoreMovie("First Movie", 101);
        var secondMovie = TestHelpers.CreateStoreMovie("Second Movie", 202);
        var list = TestHelpers.CreateList("Top 100");
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetListCountAsync("Top 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        rankingRepository.Setup(repository => repository.GetMoviesForListByPageAsync("Top 100", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ListPageMovie>
            {
                new(firstMovie.MovieId, firstMovie.Title, firstMovie.TmdbId, 1),
                new(secondMovie.MovieId, secondMovie.Title, secondMovie.TmdbId, 2)
            });

        var listRepository = new Mock<IListRepository>();
        listRepository.Setup(repository => repository.GetListByNameAsync("Top 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var movieCalls = 0;
        var ratingCalls = 0;
        var service = CreateService(
            rankingRepository: rankingRepository,
            listRepository: listRepository,
            getImdbRating: _ =>
            {
                ratingCalls++;
                return Task.FromResult<decimal?>(8.1m);
            },
            getMovieById: id =>
            {
                movieCalls++;
                return Task.FromResult(TestHelpers.CreateTmdbMovie(id: id, title: id == 101 ? "First Movie" : "Second Movie"));
            });

        var firstResult = await service.BuildMovieList("Top 100", 1);
        var secondResult = await service.BuildMovieList("Top 100", 1);

        Assert.Equal(new[] { 1, 2 }, firstResult.Movies.Select(movie => movie.Ranking));
        Assert.Equal(new[] { "First Movie", "Second Movie" }, firstResult.Movies.Select(movie => movie.Title));
        Assert.Equal(new[] { 1, 2 }, secondResult.Movies.Select(movie => movie.Ranking));
        Assert.Equal(2, movieCalls);
        Assert.Equal(2, ratingCalls);
    }

    [Fact]
    public async Task BuildMovieDetail_ComposesDetailAndReusesCreditsForDirectorAndActors()
    {
        var tmdbMovie = TestHelpers.CreateTmdbMovie(id: 42, title: "Alien");
        var list = TestHelpers.CreateList("Top 100");
        var storeMovie = TestHelpers.CreateStoreMovie("Alien", 42);
        var listRankingId = Guid.NewGuid();
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("Alien", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storeMovie);
        var listRepository = new Mock<IListRepository>();
        listRepository.Setup(repository => repository.GetAllListsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EatMyMovies.DataAccess.Models.List> { list });
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetListRankingsForMovieAsync(storeMovie.MovieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieRankingSummary>
            {
                new(storeMovie.MovieId, list.ListId, list.Name, 1, listRankingId)
            });

        var creditsCalls = 0;
        var service = CreateService(
            rankingRepository: rankingRepository,
            listRepository: listRepository,
            movieRepository: movieRepository,
            getImdbRating: _ => Task.FromResult<decimal?>(8.5m),
            getMovieById: _ => Task.FromResult(tmdbMovie),
            getMovieVideos: _ => Task.FromResult(new ResultContainer<Video>
            {
                Results = new List<Video> { new Video { Type = "Trailer", Key = "abc123" } }
            }),
            getCredits: _ =>
            {
                creditsCalls++;
                return Task.FromResult(new Credits
                {
                    Crew = new List<Crew>
                    {
                        new Crew { Id = 9, Name = "Ridley Scott", Job = "Director", ProfilePath = "/ridley.jpg" }
                    },
                    Cast = new List<Cast>
                    {
                        new Cast { Id = 10, Name = "Sigourney Weaver", KnownForDepartment = "Acting", ProfilePath = "/sigourney.jpg", Character = "Ripley" }
                    }
                });
            },
            getPerson: _ => Task.FromResult<TmdbPerson?>(new TmdbPerson { Biography = "Director bio." }));

        var detail = await service.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: true);
        var director = await service.GetDirector(42);

        Assert.Equal("Alien", detail.Title);
        Assert.Equal("https://www.youtube.com/embed/abc123", detail.TrailerPath);
        Assert.Equal(8.5m, detail.ImdbRating);
        Assert.Equal("Ridley Scott", detail.Director.Name);
        Assert.Single(detail.Actors);
        Assert.Equal("Sigourney Weaver", detail.Actors[0].Name);
        Assert.Single(detail.Lists);
        Assert.Single(detail.Rankings);
        Assert.Equal("Ridley Scott", director.Name);
        Assert.Equal(1, creditsCalls);
    }

    [Fact]
    public async Task BuildMovieOfTheWeekAsync_ReturnsNullWhenSelectionDoesNotExist()
    {
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        movieOfTheWeekRepository.Setup(repository => repository.GetSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EatMyMovies.DataAccess.Models.MovieOfTheWeekSelection?)null);
        var service = CreateService(movieOfTheWeekRepository: movieOfTheWeekRepository);

        var movie = await service.BuildMovieOfTheWeekAsync();

        Assert.Null(movie);
    }

    [Fact]
    public async Task BuildMovieOfTheWeekAsync_MapsSelectedTmdbMovie()
    {
        var storedMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        movieOfTheWeekRepository.Setup(repository => repository.GetSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EatMyMovies.DataAccess.Models.MovieOfTheWeekSelection
            {
                MovieId = storedMovie.MovieId,
                Movie = storedMovie,
                UpdatedUtc = DateTime.UtcNow
            });
        var service = CreateService(
            movieOfTheWeekRepository: movieOfTheWeekRepository,
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 348, title: "Alien")),
            getImdbRating: _ => Task.FromResult<decimal?>(8.5m),
            getCredits: _ => Task.FromResult(new Credits
            {
                Crew = new List<Crew>
                {
                    new Crew { Id = 9, Name = "Ridley Scott", Job = "Director" }
                },
                Cast = new List<Cast>()
            }));

        var movie = await service.BuildMovieOfTheWeekAsync();

        Assert.NotNull(movie);
        Assert.Equal("Alien", movie.Title);
        Assert.Equal(348, movie.TmdbId);
        Assert.Equal(8.5m, movie.ImdbRating);
        Assert.Equal("Ridley Scott", movie.Director);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("7.0")]
    public async Task GetImdbRating_CachesRatings(string? expectedRating)
    {
        decimal? ratingValue = expectedRating == null ? null : decimal.Parse(expectedRating);
        var requests = 0;
        var service = CreateService(getImdbRating: _ =>
        {
            requests++;
            return Task.FromResult(ratingValue);
        });

        var firstRating = await service.GetImdbRating("Movie Title");
        var secondRating = await service.GetImdbRating("  movie title  ");

        Assert.Equal(ratingValue, firstRating);
        Assert.Equal(ratingValue, secondRating);
        Assert.Equal(1, requests);
    }

    private static MovieService CreateService(
        Mock<IRankingRepository>? rankingRepository = null,
        Mock<IListRepository>? listRepository = null,
        Mock<IMovieRepository>? movieRepository = null,
        Mock<IMovieOfTheWeekRepository>? movieOfTheWeekRepository = null,
        Func<string, Task<SearchContainer<SearchMovie>>>? searchMovies = null,
        Func<string, Task<TmdbMovie>>? getMovieByTitle = null,
        Func<int, Task<TmdbMovie>>? getMovieById = null,
        Func<int, Task<ResultContainer<Video>>>? getMovieVideos = null,
        Func<int, Task<Credits>>? getCredits = null,
        Func<int, Task<TmdbPerson?>>? getPerson = null,
        Func<string, Task<decimal?>>? getImdbRating = null)
    {
        return new MovieService(
            (rankingRepository ?? new Mock<IRankingRepository>()).Object,
            (listRepository ?? new Mock<IListRepository>()).Object,
            (movieRepository ?? new Mock<IMovieRepository>()).Object,
            (movieOfTheWeekRepository ?? new Mock<IMovieOfTheWeekRepository>()).Object,
            new MemoryCache(new MemoryCacheOptions()),
            new FakeTmdbMovieClient(
                searchMovies,
                getMovieByTitle,
                getMovieById,
                getMovieVideos,
                getCredits,
                getPerson),
            new FakeOmdbClient(getImdbRating),
            Options.Create(new MovieExternalApiOptions()));
    }

    private sealed class FakeTmdbMovieClient : ITmdbMovieClient
    {
        private readonly Func<string, Task<SearchContainer<SearchMovie>>> _searchMovies;
        private readonly Func<string, Task<TmdbMovie>> _getMovieByTitle;
        private readonly Func<int, Task<TmdbMovie>> _getMovieById;
        private readonly Func<int, Task<ResultContainer<Video>>> _getMovieVideos;
        private readonly Func<int, Task<Credits>> _getCredits;
        private readonly Func<int, Task<TmdbPerson?>> _getPerson;

        public FakeTmdbMovieClient(
            Func<string, Task<SearchContainer<SearchMovie>>>? searchMovies,
            Func<string, Task<TmdbMovie>>? getMovieByTitle,
            Func<int, Task<TmdbMovie>>? getMovieById,
            Func<int, Task<ResultContainer<Video>>>? getMovieVideos,
            Func<int, Task<Credits>>? getCredits,
            Func<int, Task<TmdbPerson?>>? getPerson)
        {
            _searchMovies = searchMovies ?? (_ => Task.FromResult(new SearchContainer<SearchMovie>
            {
                Results = new List<SearchMovie> { new SearchMovie { Id = 1 } }
            }));
            _getMovieByTitle = getMovieByTitle ?? (_ => Task.FromResult(TestHelpers.CreateTmdbMovie()));
            _getMovieById = getMovieById ?? (id => Task.FromResult(TestHelpers.CreateTmdbMovie(id: id)));
            _getMovieVideos = getMovieVideos ?? (_ => Task.FromResult(new ResultContainer<Video>
            {
                Results = new List<Video>()
            }));
            _getCredits = getCredits ?? (_ => Task.FromResult(new Credits { Cast = new List<Cast>(), Crew = new List<Crew>() }));
            _getPerson = getPerson ?? (_ => Task.FromResult<TmdbPerson?>(null));
        }

        public Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title)
        {
            return _searchMovies(title);
        }

        public Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title, int page)
        {
            return _searchMovies(title);
        }

        public async Task<TmdbMovie> GetMovieByIdAsync(int id)
        {
            if (id == 1)
            {
                return await _getMovieByTitle(string.Empty);
            }

            return await _getMovieById(id);
        }

        public Task<ResultContainer<Video>> GetMovieVideosAsync(int movieId)
        {
            return _getMovieVideos(movieId);
        }

        public Task<Credits> GetMovieCreditsAsync(int movieId)
        {
            return _getCredits(movieId);
        }

        public Task<TmdbPerson?> GetPersonAsync(int personId)
        {
            return _getPerson(personId);
        }
    }

    private sealed class FakeOmdbClient : IOmdbClient
    {
        private readonly Func<string, Task<decimal?>> _getImdbRating;

        public FakeOmdbClient(Func<string, Task<decimal?>>? getImdbRating)
        {
            _getImdbRating = getImdbRating ?? (_ => Task.FromResult<decimal?>(7.0m));
        }

        public Task<decimal?> GetImdbRatingAsync(string movieTitle)
        {
            return _getImdbRating(movieTitle);
        }
    }
}
