using EatMyMoviesSite.Options;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMoviesSite.Services
{
    internal sealed class TmdbMovieClient : ITmdbMovieClient
    {
        private readonly TMDbClient _tmdbClient;
        private readonly int _maxRetryAttempts;
        private readonly Func<string, Task<SearchContainer<SearchMovie>>> _searchMovies;
        private readonly Func<string, int, Task<SearchContainer<SearchMovie>>> _searchMoviesByPage;
        private readonly Func<int, Task<Movie>> _getMovieById;
        private readonly Func<int, Task<ResultContainer<Video>>> _getMovieVideos;
        private readonly Func<int, Task<Credits>> _getMovieCredits;
        private readonly Func<int, Task<TmdbPerson?>> _getPerson;

        public TmdbMovieClient(IOptions<TmdbOptions> options)
            : this(options.Value, null, null, null, null, null, null)
        {
        }

        internal TmdbMovieClient(
            TmdbOptions options,
            Func<string, Task<SearchContainer<SearchMovie>>>? searchMovies,
            Func<string, int, Task<SearchContainer<SearchMovie>>>? searchMoviesByPage,
            Func<int, Task<Movie>>? getMovieById,
            Func<int, Task<ResultContainer<Video>>>? getMovieVideos,
            Func<int, Task<Credits>>? getMovieCredits,
            Func<int, Task<TmdbPerson?>>? getPerson)
        {
            _maxRetryAttempts = options.MaxRetryAttempts;
            _tmdbClient = new TMDbClient(options.ApiKey)
            {
                MaxRetryCount = options.MaxRetryAttempts,
                Timeout = options.Timeout
            };

            _searchMovies = searchMovies ?? SearchMoviesWithClientAsync;
            _searchMoviesByPage = searchMoviesByPage ?? SearchMoviesWithClientAsync;
            _getMovieById = getMovieById ?? GetMovieByIdWithClientAsync;
            _getMovieVideos = getMovieVideos ?? GetMovieVideosWithClientAsync;
            _getMovieCredits = getMovieCredits ?? GetMovieCreditsWithClientAsync;
            _getPerson = getPerson ?? (personId => _tmdbClient.GetPersonAsync(personId));
        }

        public Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title)
        {
            return ExecuteTmdbRequestAsync(
                () => _searchMovies(title),
                $"searching for movie title '{title}'");
        }

        public Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title, int page)
        {
            return ExecuteTmdbRequestAsync(
                () => _searchMoviesByPage(title, page),
                $"searching for movie title '{title}'");
        }

        public Task<Movie> GetMovieByIdAsync(int id)
        {
            return ExecuteTmdbRequestAsync(
                () => _getMovieById(id),
                $"getting movie {id}");
        }

        public Task<ResultContainer<Video>> GetMovieVideosAsync(int movieId)
        {
            return ExecuteTmdbRequestAsync(
                () => _getMovieVideos(movieId),
                $"getting videos for movie {movieId}");
        }

        public Task<Credits> GetMovieCreditsAsync(int movieId)
        {
            return ExecuteTmdbRequestAsync(
                () => _getMovieCredits(movieId),
                $"getting credits for movie {movieId}");
        }

        public Task<TmdbPerson?> GetPersonAsync(int personId)
        {
            return ExecuteTmdbRequestAsync(
                () => _getPerson(personId),
                $"getting person {personId}");
        }

        private async Task<T> ExecuteTmdbRequestAsync<T>(Func<Task<T>> request, string operation)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await request();
                }
                catch (Exception ex) when (attempt < _maxRetryAttempts && IsTransientTmdbException(ex))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                }
                catch (Exception ex) when (IsTransientTmdbException(ex))
                {
                    throw new HttpRequestException(
                        $"TMDb request failed while {operation} after {_maxRetryAttempts} attempts.",
                        ex);
                }
            }
        }

        private static bool IsTransientTmdbException(Exception exception)
        {
            return exception is HttpRequestException ||
                   exception is IOException ||
                   exception.InnerException != null && IsTransientTmdbException(exception.InnerException);
        }

        private async Task<SearchContainer<SearchMovie>> SearchMoviesWithClientAsync(string title)
        {
            return await _tmdbClient.SearchMovieAsync(title)
                ?? throw new InvalidOperationException("TMDb returned an empty movie search response.");
        }

        private async Task<SearchContainer<SearchMovie>> SearchMoviesWithClientAsync(string title, int page)
        {
            return await _tmdbClient.SearchMovieAsync(title, page)
                ?? throw new InvalidOperationException("TMDb returned an empty movie search response.");
        }

        private async Task<Movie> GetMovieByIdWithClientAsync(int id)
        {
            return await _tmdbClient.GetMovieAsync(id)
                ?? throw new InvalidOperationException($"TMDb returned an empty movie response for {id}.");
        }

        private async Task<ResultContainer<Video>> GetMovieVideosWithClientAsync(int movieId)
        {
            return await _tmdbClient.GetMovieVideosAsync(movieId)
                ?? throw new InvalidOperationException($"TMDb returned an empty videos response for movie {movieId}.");
        }

        private async Task<Credits> GetMovieCreditsWithClientAsync(int movieId)
        {
            return await _tmdbClient.GetMovieCreditsAsync(movieId)
                ?? throw new InvalidOperationException($"TMDb returned an empty credits response for movie {movieId}.");
        }
    }
}
