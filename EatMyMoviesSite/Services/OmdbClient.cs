using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EatMyMoviesSite.Options;
using Microsoft.Extensions.Options;

namespace EatMyMoviesSite.Services
{
    internal sealed class OmdbClient : IOmdbClient
    {
        public const string HttpClientName = "Omdb";

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OmdbClient(IHttpClientFactory httpClientFactory, IOptions<OmdbOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient(HttpClientName);
            _apiKey = options.Value.ApiKey;
        }

        internal OmdbClient(HttpClient httpClient, OmdbOptions options)
        {
            _httpClient = httpClient;
            _apiKey = options.ApiKey;
        }

        public async Task<decimal?> GetImdbRatingAsync(string movieTitle)
        {
            try
            {
                var movie = await _httpClient.GetFromJsonAsync<OmdbMovieResponse>(
                    $"?apikey={Uri.EscapeDataString(_apiKey)}&t={Uri.EscapeDataString(movieTitle)}");

                if (movie?.ImdbRating != null &&
                    movie.ImdbRating != "N/A" &&
                    decimal.TryParse(movie.ImdbRating, CultureInfo.InvariantCulture, out var imdbRating))
                {
                    return imdbRating;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private sealed class OmdbMovieResponse
        {
            [JsonPropertyName("imdbRating")]
            public string? ImdbRating { get; set; }
        }
    }
}
