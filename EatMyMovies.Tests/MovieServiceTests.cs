using EatMyMovies.DataAccess.QueryModels;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Collections.Concurrent;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TmdbMovie = TMDbLib.Objects.Movies.Movie;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMovies.Tests;

public class MovieServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task BuildMovieList_InvalidPage_DoesNotLoadRowsOrExternalDetails(int page)
    {
        var rankings = new Mock<IRankingRepository>(MockBehavior.Strict);
        rankings.Setup(x => x.GetListCountAsync("Top 100", It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var lists = new Mock<IListRepository>();
        lists.Setup(x => x.GetListByNameAsync("Top 100", It.IsAny<CancellationToken>())).ReturnsAsync(TestHelpers.CreateList("Top 100"));
        var service = CreateService(rankingRepository: rankings, listRepository: lists,
            getMovieById: _ => throw new InvalidOperationException("Must not load movie data."));
        await Assert.ThrowsAsync<InvalidListPageException>(() => service.BuildMovieList("Top 100", page));
        rankings.Verify(x => x.GetListCountAsync("Top 100", It.IsAny<CancellationToken>()), Times.Once);
        rankings.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildMovieList_EmptyList_HasValidFirstPage()
    {
        var rankings = new Mock<IRankingRepository>();
        rankings.Setup(x => x.GetMoviesForListByPageAsync("Top 100", 1, 10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var lists = new Mock<IListRepository>();
        lists.Setup(x => x.GetListByNameAsync("Top 100", It.IsAny<CancellationToken>())).ReturnsAsync(TestHelpers.CreateList("Top 100"));
        var service = CreateService(rankingRepository: rankings, listRepository: lists);
        var result = await service.BuildMovieList("Top 100", 1);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(1, result.CurrentPage);
        Assert.Empty(result.Movies);
    }

    [Fact]
    public async Task GetMovieByTitle_EmptySearch_IsMissing_AndFailureIsNotCached()
    {
        var attempts = 0;
        var service = CreateService(searchMovies: _ =>
        {
            attempts++;
            return Task.FromResult(new SearchContainer<SearchMovie> { Results = [] });
        });
        await Assert.ThrowsAsync<MovieNotFoundException>(() => service.GetMovieByTitle("Missing"));
        await Assert.ThrowsAsync<MovieNotFoundException>(() => service.GetMovieByTitle("Missing"));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SearchMoviesByTitle_PreservesIdentityAndMapsOptionalReleaseYearWithoutDetailRequests()
    {
        var searches = 0;
        var detailRequests = 0;
        var service = CreateService(
            searchMovies: title =>
            {
                Assert.Equal("Dune", title);
                searches++;
                return Task.FromResult(new SearchContainer<SearchMovie>
                {
                    Results = new List<SearchMovie>
                    {
                        new() { Id = 438631, Title = "Dune", PosterPath = "/new.jpg", ReleaseDate = new DateTime(2021, 9, 15) },
                        new() { Id = 841, Title = "Dune", PosterPath = "/old.jpg", ReleaseDate = new DateTime(1984, 12, 14) },
                        new() { Id = 999, Title = "Undated Film", PosterPath = "/unknown.jpg", ReleaseDate = null }
                    }
                });
            },
            getMovieById: id =>
            {
                detailRequests++;
                return Task.FromResult(TestHelpers.CreateTmdbMovie(id: id));
            });

        var results = await service.SearchMoviesByTitle("Dune");

        Assert.Equal(new[] { 438631, 841, 999 }, results.Select(movie => movie.Id));
        Assert.Equal(new int?[] { 2021, 1984, null }, results.Select(movie => movie.ReleaseYear));
        Assert.Equal(new[] { "Dune", "Dune", "Undated Film" }, results.Select(movie => movie.Title));
        Assert.Equal(new[] { "/new.jpg", "/old.jpg", "/unknown.jpg" }, results.Select(movie => movie.PosterPath));
        Assert.Equal(1, searches);
        Assert.Equal(0, detailRequests);
    }

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

    [Fact]
    public async Task BuildMovieDetail_MergesDeduplicatesOrdersCapsAndCachesStreamingProviders()
    {
        var requests = 0;
        var service = CreateService(
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 42, title: "Alien")),
            getWatchProviders: (_, _) =>
            {
                requests++;
                return Task.FromResult(new SingleResultContainer<Dictionary<string, WatchProviders>>
                {
                    Results = new Dictionary<string, WatchProviders>
                    {
                        ["GB"] = new WatchProviders
                        {
                            Link = "https://www.themoviedb.org/movie/348/watch?locale=GB",
                            FlatRate = new List<WatchProviderItem>
                            {
                                CreateWatchProvider(9, "Provider B", 3, "/provider-b.jpg"),
                                CreateWatchProvider(8, "Provider A", 4, "/provider-a.jpg"),
                                CreateWatchProvider(10, "Duplicate Provider", 5, "/duplicate-late.jpg"),
                                CreateWatchProvider(20, "No Logo", 0)
                            },
                            Free = new List<WatchProviderItem>
                            {
                                CreateWatchProvider(10, "Duplicate Provider", 1, "/duplicate.jpg"),
                                new WatchProviderItem
                                {
                                    ProviderName = "Name Only",
                                    DisplayPriority = 2,
                                    LogoPath = "/name-only.jpg"
                                }
                            },
                            Ads = new List<WatchProviderItem>
                            {
                                new WatchProviderItem
                                {
                                    ProviderName = " name only ",
                                    DisplayPriority = 6,
                                    LogoPath = "/name-only-late.jpg"
                                },
                                CreateWatchProvider(11, "Ads Provider", 7, "/ads.jpg")
                            },
                            Rent = new List<WatchProviderItem> { CreateWatchProvider(12, "Rental Provider", 0, "/rent.jpg") },
                            Buy = new List<WatchProviderItem> { CreateWatchProvider(13, "Purchase Provider", 0, "/buy.jpg") }
                        },
                        ["US"] = new WatchProviders
                        {
                            Link = "https://www.themoviedb.org/movie/348/watch?locale=US",
                            FlatRate = new List<WatchProviderItem> { CreateWatchProvider(99, "US Only", 0, "/us.jpg") }
                        }
                    }
                });
            });

        var first = await service.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: false);
        var second = await service.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: false);

        Assert.Equal("https://www.themoviedb.org/movie/348/watch?locale=GB", first.WatchAvailability.Link);
        Assert.Equal(
            new[] { "Duplicate Provider", "Name Only", "Provider B" },
            first.WatchAvailability.Providers.Select(provider => provider.Name));
        Assert.Equal(
            "https://image.tmdb.org/t/p/w92/duplicate.jpg",
            first.WatchAvailability.Providers[0].LogoUrl);
        Assert.DoesNotContain(first.WatchAvailability.Providers, provider =>
            provider.Name is "Rental Provider" or "Purchase Provider" or "No Logo" or "US Only");
        Assert.Equal(first.WatchAvailability.Link, second.WatchAvailability.Link);
        Assert.Equal(
            first.WatchAvailability.Providers.Select(provider => provider.Name),
            second.WatchAvailability.Providers.Select(provider => provider.Name));
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task BuildMovieDetail_HidesMissingAndFailedWatchProviderData()
    {
        var noDataService = CreateService(
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 42, title: "Alien")),
            getWatchProviders: (_, _) => Task.FromResult(new SingleResultContainer<Dictionary<string, WatchProviders>>
            {
                Results = new Dictionary<string, WatchProviders>
                {
                    ["US"] = new WatchProviders
                    {
                        FlatRate = new List<WatchProviderItem> { CreateWatchProvider(99, "US Only", 1) }
                    }
                }
            }));
        var failedService = CreateService(
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 42, title: "Alien")),
            getWatchProviders: (_, _) => throw new HttpRequestException("Watch provider request failed."));

        var noDataDetail = await noDataService.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: false);
        var failedDetail = await failedService.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: false);

        Assert.Null(noDataDetail.WatchAvailability.Link);
        Assert.Empty(noDataDetail.WatchAvailability.Providers);
        Assert.Null(failedDetail.WatchAvailability.Link);
        Assert.Empty(failedDetail.WatchAvailability.Providers);
        Assert.Equal("Alien", failedDetail.Title);
    }

    [Theory]
    [InlineData(false, "00:30:00")]
    [InlineData(true, "00:15:00")]
    public async Task BuildMovieDetail_UsesShorterWatchProviderCacheForNoDataAndFailures(
        bool requestFails,
        string expectedDuration)
    {
        var cache = new RecordingMemoryCache();
        var options = new MovieExternalApiOptions
        {
            WatchProviderCacheDuration = TimeSpan.FromHours(6),
            UnknownWatchProviderCacheDuration = TimeSpan.FromMinutes(30),
            WatchProviderFailureCacheDuration = TimeSpan.FromMinutes(15)
        };
        var service = CreateService(
            memoryCache: cache,
            externalApiOptions: options,
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 42, title: "Alien")),
            getWatchProviders: requestFails
                ? (_, _) => throw new HttpRequestException("Watch provider request failed.")
                : (_, _) => Task.FromResult(new SingleResultContainer<Dictionary<string, WatchProviders>>
                {
                    Results = new Dictionary<string, WatchProviders>()
                }));

        await service.BuildMovieDetail(title: null, tmdbId: 42, includeListContext: false);

        Assert.Equal(TimeSpan.Parse(expectedDuration), cache.Durations["tmdb:watch-providers:GB:42"]);
    }

    [Fact]
    public async Task BuildMovieDetail_PropagatesWatchProviderCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var service = CreateService(
            getMovieById: _ => Task.FromResult(TestHelpers.CreateTmdbMovie(id: 42, title: "Alien")),
            getWatchProviders: (_, cancellationToken) => Task.FromCanceled<SingleResultContainer<Dictionary<string, WatchProviders>>>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.BuildMovieDetail(
            title: null,
            tmdbId: 42,
            includeListContext: false,
            cancellationSource.Token));
    }

    private static WatchProviderItem CreateWatchProvider(int id, string name, int priority, string? logoPath = null)
    {
        return new WatchProviderItem
        {
            ProviderId = id,
            ProviderName = name,
            DisplayPriority = priority,
            LogoPath = logoPath
        };
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
        Func<int, CancellationToken, Task<SingleResultContainer<Dictionary<string, WatchProviders>>>>? getWatchProviders = null,
        Func<int, Task<TmdbPerson?>>? getPerson = null,
        Func<string, Task<decimal?>>? getImdbRating = null,
        IMemoryCache? memoryCache = null,
        MovieExternalApiOptions? externalApiOptions = null)
    {
        return new MovieService(
            (rankingRepository ?? new Mock<IRankingRepository>()).Object,
            (listRepository ?? new Mock<IListRepository>()).Object,
            (movieRepository ?? new Mock<IMovieRepository>()).Object,
            (movieOfTheWeekRepository ?? new Mock<IMovieOfTheWeekRepository>()).Object,
            memoryCache ?? new MemoryCache(new MemoryCacheOptions()),
            new FakeTmdbMovieClient(
                searchMovies,
                getMovieByTitle,
                getMovieById,
                getMovieVideos,
                getCredits,
                getWatchProviders,
                getPerson),
            new FakeOmdbClient(getImdbRating),
            Options.Create(externalApiOptions ?? new MovieExternalApiOptions()),
            NullLogger<MovieService>.Instance);
    }

    private sealed class FakeTmdbMovieClient : ITmdbMovieClient
    {
        private readonly Func<string, Task<SearchContainer<SearchMovie>>> _searchMovies;
        private readonly Func<string, Task<TmdbMovie>> _getMovieByTitle;
        private readonly Func<int, Task<TmdbMovie>> _getMovieById;
        private readonly Func<int, Task<ResultContainer<Video>>> _getMovieVideos;
        private readonly Func<int, Task<Credits>> _getCredits;
        private readonly Func<int, CancellationToken, Task<SingleResultContainer<Dictionary<string, WatchProviders>>>> _getWatchProviders;
        private readonly Func<int, Task<TmdbPerson?>> _getPerson;

        public FakeTmdbMovieClient(
            Func<string, Task<SearchContainer<SearchMovie>>>? searchMovies,
            Func<string, Task<TmdbMovie>>? getMovieByTitle,
            Func<int, Task<TmdbMovie>>? getMovieById,
            Func<int, Task<ResultContainer<Video>>>? getMovieVideos,
            Func<int, Task<Credits>>? getCredits,
            Func<int, CancellationToken, Task<SingleResultContainer<Dictionary<string, WatchProviders>>>>? getWatchProviders,
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
            _getWatchProviders = getWatchProviders ?? ((_, _) => Task.FromResult(new SingleResultContainer<Dictionary<string, WatchProviders>>
            {
                Results = new Dictionary<string, WatchProviders>()
            }));
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

        public Task<SingleResultContainer<Dictionary<string, WatchProviders>>> GetMovieWatchProvidersAsync(
            int movieId,
            CancellationToken cancellationToken = default)
        {
            return _getWatchProviders(movieId, cancellationToken);
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

    private sealed class RecordingMemoryCache : IMemoryCache
    {
        private static readonly object NullValue = new();
        private readonly ConcurrentDictionary<object, object?> _values = new();

        public ConcurrentDictionary<object, TimeSpan?> Durations { get; } = new();

        public bool TryGetValue(object key, out object? value)
        {
            if (!_values.TryGetValue(key, out value))
            {
                return false;
            }

            if (ReferenceEquals(value, NullValue))
            {
                value = null;
            }

            return true;
        }

        public ICacheEntry CreateEntry(object key)
        {
            return new RecordingCacheEntry(key, this);
        }

        public void Remove(object key)
        {
            _values.TryRemove(key, out _);
            Durations.TryRemove(key, out _);
        }

        public void Dispose()
        {
        }

        private void Store(RecordingCacheEntry entry)
        {
            _values[entry.Key] = entry.Value ?? NullValue;
            Durations[entry.Key] = entry.AbsoluteExpirationRelativeToNow;
        }

        private sealed class RecordingCacheEntry : ICacheEntry
        {
            private readonly RecordingMemoryCache _owner;

            public RecordingCacheEntry(object key, RecordingMemoryCache owner)
            {
                Key = key;
                _owner = owner;
            }

            public object Key { get; }
            public object? Value { get; set; }
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
            public CacheItemPriority Priority { get; set; }
            public long? Size { get; set; }

            public void Dispose()
            {
                _owner.Store(this);
            }
        }
    }
}
