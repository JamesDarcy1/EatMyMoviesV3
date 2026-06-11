using System.ComponentModel.DataAnnotations;
using System.Net;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;

namespace EatMyMovies.Tests;

public class ExternalMovieClientTests
{
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
            getPerson: null);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieByIdAsync(42));

        Assert.Equal(2, attempts);
        Assert.Contains("TMDb request failed while getting movie 42 after 2 attempts.", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public void ApiOptions_RequireApiKeys()
    {
        Assert.Contains(Validate(new TmdbOptions()), result => result.MemberNames.Contains(nameof(TmdbOptions.ApiKey)));
        Assert.Contains(Validate(new OmdbOptions()), result => result.MemberNames.Contains(nameof(OmdbOptions.ApiKey)));
    }

    private static List<ValidationResult> Validate(object options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
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
