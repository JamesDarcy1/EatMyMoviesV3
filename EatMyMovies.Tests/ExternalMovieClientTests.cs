using System.ComponentModel.DataAnnotations;
using System.Net;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using TMDbLib.Objects.General;

namespace EatMyMovies.Tests;

public class ExternalMovieClientTests
{
    [Fact]
    public async Task TmdbMovieClient_ConfirmedMissingMovie_IsNotRetried()
    {
        var attempts = 0;
        var client = MovieFailureClient(() =>
        {
            attempts++;
            return new TMDbLib.Objects.Exceptions.NotFoundException(new TMDbLib.Objects.Exceptions.TMDbStatusMessage());
        });
        await Assert.ThrowsAsync<MovieNotFoundException>(() => client.GetMovieByIdAsync(404));
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData("rate-limit")]
    [InlineData("http-503")]
    [InlineData("timeout")]
    public async Task TmdbMovieClient_UnavailableData_IsTypedFor503(string failure)
    {
        var client = MovieFailureClient(() => failure switch
        {
            "rate-limit" => new TMDbLib.Objects.Exceptions.GeneralHttpException(HttpStatusCode.TooManyRequests),
            "http-503" => new TMDbLib.Objects.Exceptions.GeneralHttpException(HttpStatusCode.ServiceUnavailable),
            _ => new TimeoutException()
        });
        await Assert.ThrowsAsync<MovieDataUnavailableException>(() => client.GetMovieByIdAsync(42));
    }

    [Fact]
    public async Task TmdbMovieClient_CancellationAndUnexpectedErrors_PropagateUnchanged()
    {
        var cancellation = new OperationCanceledException();
        var client = MovieFailureClient(() => cancellation);
        Assert.Same(cancellation, await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetMovieByIdAsync(42)));
        var unexpected = new InvalidOperationException();
        client = MovieFailureClient(() => unexpected);
        Assert.Same(unexpected, await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetMovieByIdAsync(42)));
    }

    private static TmdbMovieClient MovieFailureClient(Func<Exception> failure) =>
        new(new TmdbOptions { ApiKey = "test-key", MaxRetryAttempts = 1 }, null, null,
            _ => throw failure(), null, null, null, null);

    [Theory]
    [InlineData("""{"imdbRating":"8.4"}""", "8.4")]
    [InlineData("""{"imdbRating":"N/A"}""", null)]
    [InlineData("""{"imdbRating":"not-a-rating"}""", null)]
    [InlineData("""{}""", null)]
    public async Task OmdbClient_ReturnsParsedRatingOrNull(string response, string? expectedRating)
    {
        var client = new OmdbClient(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            }))
            {
                BaseAddress = new Uri("https://www.omdbapi.com/")
            },
            new OmdbOptions { ApiKey = "omdb-key" });

        var rating = await client.GetImdbRatingAsync("Alien");

        Assert.Equal(expectedRating == null ? null : decimal.Parse(expectedRating), rating);
    }

    [Fact]
    public async Task OmdbClient_ReturnsNullWhenRequestFails()
    {
        var client = new OmdbClient(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)))
            {
                BaseAddress = new Uri("https://www.omdbapi.com/")
            },
            new OmdbOptions { ApiKey = "omdb-key" });

        var rating = await client.GetImdbRatingAsync("Alien");

        Assert.Null(rating);
    }

    [Fact]
    public async Task TmdbMovieClient_RetriesTransientFailuresAndWrapsFinalFailure()
    {
        var attempts = 0;
        var client = new TmdbMovieClient(
            new TmdbOptions { ApiKey = "tmdb-key", MaxRetryAttempts = 2 },
            searchMovies: null,
            searchMoviesByPage: null,
            getMovieById: _ =>
            {
                attempts++;
                throw new HttpRequestException("Temporary TMDb failure.");
            },
            getMovieVideos: null,
            getMovieCredits: null,
            getMovieWatchProviders: null,
            getPerson: null);

        var exception = await Assert.ThrowsAsync<MovieDataUnavailableException>(() => client.GetMovieByIdAsync(42));

        Assert.Equal(2, attempts);
        Assert.Contains("TMDb request failed while getting movie 42 after 2 attempts.", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task TmdbMovieClient_ReturnsWatchProvidersAndPropagatesCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var expected = new SingleResultContainer<Dictionary<string, WatchProviders>>
        {
            Results = new Dictionary<string, WatchProviders>
            {
                ["GB"] = new WatchProviders { Link = "https://www.themoviedb.org/watch" }
            }
        };
        CancellationToken observedToken = default;
        var client = CreateTmdbClient((movieId, cancellationToken) =>
        {
            Assert.Equal(42, movieId);
            observedToken = cancellationToken;
            return Task.FromResult(expected);
        });

        var result = await client.GetMovieWatchProvidersAsync(42, cancellationSource.Token);

        Assert.Same(expected, result);
        Assert.Equal(cancellationSource.Token, observedToken);
    }

    [Fact]
    public async Task TmdbMovieClient_RetriesTransientWatchProviderFailure()
    {
        var attempts = 0;
        var client = CreateTmdbClient((_, _) =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new HttpRequestException("Temporary TMDb failure.");
            }

            return Task.FromResult(new SingleResultContainer<Dictionary<string, WatchProviders>>
            {
                Results = new Dictionary<string, WatchProviders>()
            });
        });

        await client.GetMovieWatchProvidersAsync(42);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public void ApiOptions_RequireApiKeys()
    {
        Assert.Contains(Validate(new TmdbOptions()), result => result.MemberNames.Contains(nameof(TmdbOptions.ApiKey)));
        Assert.Contains(Validate(new OmdbOptions()), result => result.MemberNames.Contains(nameof(OmdbOptions.ApiKey)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("GBR")]
    [InlineData("G1")]
    public void MovieExternalApiOptions_RequireTwoLetterWatchProviderRegion(string region)
    {
        var options = new MovieExternalApiOptions { WatchProviderRegion = region };

        Assert.Contains(
            Validate(options),
            result => result.MemberNames.Contains(nameof(MovieExternalApiOptions.WatchProviderRegion)));
    }

    [Fact]
    public void MovieExternalApiOptions_RequirePositiveWatchProviderCacheDurations()
    {
        var options = new MovieExternalApiOptions
        {
            WatchProviderCacheDuration = TimeSpan.Zero,
            UnknownWatchProviderCacheDuration = TimeSpan.Zero,
            WatchProviderFailureCacheDuration = TimeSpan.Zero
        };
        var results = Validate(options);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MovieExternalApiOptions.WatchProviderCacheDuration)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MovieExternalApiOptions.UnknownWatchProviderCacheDuration)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MovieExternalApiOptions.WatchProviderFailureCacheDuration)));
    }

    private static List<ValidationResult> Validate(object options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    private static TmdbMovieClient CreateTmdbClient(
        Func<int, CancellationToken, Task<SingleResultContainer<Dictionary<string, WatchProviders>>>> getMovieWatchProviders)
    {
        return new TmdbMovieClient(
            new TmdbOptions { ApiKey = "tmdb-key", MaxRetryAttempts = 2 },
            searchMovies: null,
            searchMoviesByPage: null,
            getMovieById: null,
            getMovieVideos: null,
            getMovieCredits: null,
            getMovieWatchProviders: getMovieWatchProviders,
            getPerson: null);
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
