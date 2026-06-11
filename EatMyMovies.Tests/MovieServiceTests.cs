using System.Net;
using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
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
        rankingRepository.Setup(repository => repository.GetAllRankingsInList(list))
            .Returns(new[]
            {
                new ListRanking { List = list, Movie = firstMovie, Ranking = 1 },
                new ListRanking { List = list, Movie = secondMovie, Ranking = 2 }
            });
        rankingRepository.Setup(repository => repository.GetListCount("Top 100"))
            .Returns(2);
        rankingRepository.Setup(repository => repository.GetMoviesForListByPage("Top 100", 1))
            .Returns(new[] { firstMovie, secondMovie });

        var listRepository = new Mock<IListRepository>();
        listRepository.Setup(repository => repository.GetListByName("Top 100"))
            .Returns(list);

        var movieCalls = 0;
        var ratingCalls = 0;
        var service = CreateService(
            rankingRepository: rankingRepository,
            listRepository: listRepository,
            httpClient: new HttpClient(new StubHttpMessageHandler(_ =>
            {
                ratingCalls++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"imdbRating":"8.1"}""")
                };
            })),
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
        var listRanking = new ListRanking
        {
            List = list,
            Movie = storeMovie,
            Ranking = 1,
            ListRankingId = Guid.NewGuid()
        };
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTitle("Alien"))
            .Returns(storeMovie);
        var listRepository = new Mock<IListRepository>();
        listRepository.Setup(repository => repository.GetAllLists())
            .Returns(new List<EatMyMovies.DataAccess.Models.List> { list });
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetListRankingsForMovie(storeMovie.MovieId))
            .Returns(new List<ListRanking> { listRanking });

        var creditsCalls = 0;
        var service = CreateService(
            rankingRepository: rankingRepository,
            listRepository: listRepository,
            movieRepository: movieRepository,
            httpClient: new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"imdbRating":"8.5"}""")
            })),
            getMovieById: _ => Task.FromResult(tmdbMovie),
            getTrailer: _ => Task.FromResult<Video?>(new Video { Type = "Trailer", Key = "abc123" }),
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

    [Theory]
    [InlineData("""{"imdbRating":"N/A"}""")]
    [InlineData("""{"imdbRating":"not-a-rating"}""")]
    [InlineData("""{}""")]
    public async Task GetImdbRating_CachesUnknownRatingsAsNull(string response)
    {
        var requests = 0;
        var service = CreateService(httpClient: new HttpClient(new StubHttpMessageHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };
        })));

        var firstRating = await service.GetImdbRating("Unknown Movie");
        var secondRating = await service.GetImdbRating("  unknown movie  ");

        Assert.Null(firstRating);
        Assert.Null(secondRating);
        Assert.Equal(1, requests);
    }

    private static MovieService CreateService(
        Mock<IRankingRepository>? rankingRepository = null,
        Mock<IListRepository>? listRepository = null,
        Mock<IMovieRepository>? movieRepository = null,
        HttpClient? httpClient = null,
        Func<string, Task<TmdbMovie>>? getMovieByTitle = null,
        Func<int, Task<TmdbMovie>>? getMovieById = null,
        Func<int, Task<Video?>>? getTrailer = null,
        Func<int, Task<Credits>>? getCredits = null,
        Func<int, Task<TmdbPerson?>>? getPerson = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tmdb:ApiKey"] = "tmdb-key",
                ["Omdb:ApiKey"] = "omdb-key",
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        return new MovieService(
            (rankingRepository ?? new Mock<IRankingRepository>()).Object,
            configuration,
            (listRepository ?? new Mock<IListRepository>()).Object,
            (movieRepository ?? new Mock<IMovieRepository>()).Object,
            new MemoryCache(new MemoryCacheOptions()),
            httpClient ?? new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"imdbRating":"7.0"}""")
            })),
            getMovieByTitle ?? (_ => Task.FromResult(TestHelpers.CreateTmdbMovie())),
            getMovieById ?? (id => Task.FromResult(TestHelpers.CreateTmdbMovie(id: id))),
            getTrailer ?? (_ => Task.FromResult<Video?>(null)),
            getCredits ?? (_ => Task.FromResult(new Credits { Cast = new List<Cast>(), Crew = new List<Crew>() })),
            getPerson ?? (_ => Task.FromResult<TmdbPerson?>(null)));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
