using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Net.Http;
using System.Text.Json.Serialization;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using Movie = TMDbLib.Objects.Movies.Movie;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMoviesSite.Services
{
    public class MovieService : IMovieService
    {
        private readonly TMDbClient _tmdbClient;
        private readonly HttpClient _httpClient;
        private readonly string _omdbApiKey;
        private readonly IRankingRepository _rankingRepository;
        private readonly IListRepository _listRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly int _moviesPerPage = 10;
        private const int TmdbMaxRetryAttempts = 3;
        private const int ExternalApiConcurrency = 4;
        private readonly bool _isDevelopment;
        private Guid ChristmasListId;
        private readonly IMemoryCache _cache;
        private readonly Func<string, Task<Movie>> _getMovieByTitle;
        private readonly Func<int, Task<Movie>> _getMovieById;
        private readonly Func<int, Task<Video?>> _getTrailer;
        private readonly Func<int, Task<Credits>> _getCredits;
        private readonly Func<int, Task<TmdbPerson?>> _getPerson;

        public MovieService(IRankingRepository rankingRepository,
                            IConfiguration configuration,
                            IListRepository listRepository,
                            IMovieRepository movieRepository,
                            IMemoryCache memoryCache,
                            IHttpClientFactory httpClientFactory)
            : this(rankingRepository,
                  configuration,
                  listRepository,
                  movieRepository,
                  memoryCache,
                  httpClientFactory.CreateClient(),
                  null,
                  null,
                  null,
                  null,
                  null)
        {
        }

        internal MovieService(IRankingRepository rankingRepository,
                            IConfiguration configuration,
                            IListRepository listRepository,
                            IMovieRepository movieRepository,
                            IMemoryCache memoryCache,
                            HttpClient httpClient,
                            Func<string, Task<Movie>>? getMovieByTitle,
                            Func<int, Task<Movie>>? getMovieById,
                            Func<int, Task<Video?>>? getTrailer,
                            Func<int, Task<Credits>>? getCredits,
                            Func<int, Task<TmdbPerson?>>? getPerson)
        {
            _isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
            _tmdbClient = new TMDbClient(configuration["Tmdb:ApiKey"])
            {
                MaxRetryCount = TmdbMaxRetryAttempts,
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient = httpClient;
            _omdbApiKey = configuration["Omdb:ApiKey"] ?? string.Empty;
            _rankingRepository = rankingRepository;
            _listRepository = listRepository;
            _movieRepository = movieRepository;
            _cache = memoryCache;
            _getMovieByTitle = getMovieByTitle ?? SearchTmdbMovieByTitleAsync;
            _getMovieById = getMovieById ?? GetTmdbMovieByIdAsync;
            _getTrailer = getTrailer ?? GetTmdbTrailerAsync;
            _getCredits = getCredits ?? GetTmdbCreditsAsync;
            _getPerson = getPerson ?? GetTmdbPersonAsync;
        }

        public async Task<Movie> GetMovieByTitle(string title)
        {
            var cacheKey = $"tmdb:movie:title:{NormalizeTitleForCache(title)}";

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                movie = await _getMovieByTitle(title);
                _cache.Set(cacheKey, movie, TimeSpan.FromHours(6));
                _cache.Set(GetMovieByIdCacheKey(movie.Id), movie, TimeSpan.FromHours(6));
            }

            return movie;
        }

        public async Task<List<MovieDropdown>> SearchMoviesByTitle(string titleSearch)
        {
            var searchResults = await ExecuteTmdbRequestAsync(
                () => _tmdbClient.SearchMovieAsync(titleSearch, 1),
                $"searching for movie title '{titleSearch}'");
            var reducedList = searchResults.Results.Where(x => x.PosterPath != null)
                                                    .Select(x =>
                                                        new MovieDropdown()
                                                        {
                                                            Id = x.Id,
                                                            Title = x.Title,
                                                            PosterPath = x.PosterPath
                                                        }).Take(5).ToList();
            return reducedList;
        }

        public async Task<Movie> GetMovieById(int id)
        {
            var cacheKey = GetMovieByIdCacheKey(id);

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                movie = await _getMovieById(id);
                _cache.Set(cacheKey, movie, TimeSpan.FromHours(6)); 
            }
            return movie;
        }

        public async Task<Video?> GetTrailer(int movieId)
        {
            var cacheKey = $"tmdb:trailer:{movieId}";

            if (!_cache.TryGetValue(cacheKey, out Video? trailer))
            {
                try
                {
                    trailer = await _getTrailer(movieId);
                    _cache.Set(cacheKey, trailer, TimeSpan.FromHours(6));
                }
                catch
                {
                    trailer = null;
                    _cache.Set(cacheKey, trailer, TimeSpan.FromMinutes(15));
                }
            }

            return trailer;
        }

        public async Task<decimal?> GetImdbRating(string movieTitle)
        {
            var cacheKey = $"omdb:rating:{NormalizeTitleForCache(movieTitle)}";

            if (!_cache.TryGetValue(cacheKey, out RatingCacheValue? cachedRating)) {
                decimal? rating = null;
                var cacheDuration = TimeSpan.FromMinutes(30);

                try
                {
                    var movie = await _httpClient.GetFromJsonAsync<OmdbMovieResponse>(
                        $"https://www.omdbapi.com/?apikey={Uri.EscapeDataString(_omdbApiKey)}&t={Uri.EscapeDataString(movieTitle)}");
                    if (movie?.ImdbRating != null && movie.ImdbRating != "N/A")
                    {
                        if (decimal.TryParse(movie.ImdbRating, CultureInfo.InvariantCulture, out var imdbRating))
                        {
                            rating = imdbRating;
                            cacheDuration = TimeSpan.FromHours(6);
                        }
                    }
                }
                catch
                {
                    rating = null;
                }

                cachedRating = new RatingCacheValue(rating);
                _cache.Set(cacheKey, cachedRating, cacheDuration);
            }
            return cachedRating.Rating;
        }

        public async Task<MovieDetail> BuildMovieDetail(string? title, int? tmdbId, bool includeListContext)
        {
            var movie = tmdbId.HasValue
                ? await GetMovieById(tmdbId.Value)
                : await GetMovieByTitle(title ?? throw new ArgumentException("A title or TMDb id is required.", nameof(title)));

            var trailerTask = GetTrailer(movie.Id);
            var ratingTask = GetImdbRating(movie.Title);
            var creditsTask = GetCreditsSafely(movie.Id);

            var credits = await creditsTask;
            var directorTask = BuildDirectorAsync(credits);
            var actors = BuildActors(credits);

            var trailer = await trailerTask;
            var rating = await ratingTask;
            var director = await directorTask;

            var movieDetail = Mapper.MapToMovieDetail(movie, trailer, rating, director, actors);

            if (includeListContext)
            {
                movieDetail.Lists = GetAllLists();

                var storeMovie = GetStoreMovieByTitle(movie.Title);
                if (storeMovie != null)
                {
                    movieDetail.Rankings = GetListRankingsForMovie(storeMovie.MovieId);
                }
            }

            return movieDetail;
        }

        public async Task<MovieList> BuildMovieList(string listTitle, int page)
        {
            try
            {
                var list = _listRepository.GetListByName(listTitle);
                var moviesList = new MovieList()
                {
                    Name = list.Name,
                    Description = list.Description,
                    Movies = new List<ListMovie>()
                };

                var rankings = _rankingRepository.GetAllRankingsInList(list);
                var totalMovies = _rankingRepository.GetListCount(listTitle);
                var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalMovies / _moviesPerPage));
                page = Math.Max(1, Math.Min(page, totalPages));
                var moviesForPage = _rankingRepository.GetMoviesForListByPage(listTitle, page).ToList();

                moviesList.TotalPages = totalPages;
                moviesList.CurrentPage = page;

                var rankingByMovieId = rankings.ToDictionary(r => r.Movie.MovieId, r => r.Ranking);
                var results = await ParallelSelectAsync(moviesForPage, ExternalApiConcurrency, async movie =>
                {
                    var ranking = rankingByMovieId[movie.MovieId];

                    var imdbTask = GetImdbRating(movie.Title);
                    var tmdbTask = GetMovieById(movie.TmdbId.Value);

                    var tmdbMovie = await tmdbTask;
                    var imdbRating = await imdbTask;

                    return Mapper.BuildListMovie(tmdbMovie, imdbRating, ranking);
                });

                moviesList.Movies.AddRange(results);

                return moviesList;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public List<EatMyMovies.DataAccess.Models.Genre> GetAllGenres()
        {
            return _movieRepository.GetAllGenres();
        }

        public async Task<List<Movie>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange)
        {
            ChristmasListId = _listRepository.GetListByName("Christmas").ListId;

            var associatedGenres = GetGenresLinkedToFeelings(feelings);
            var storeMovies = new HashSet<EatMyMovies.DataAccess.Models.Movie>();
            var tasks = new List<Task<Movie>>();

            var moviesBeloningToGenres = _movieRepository.GetMoviesOfGenres(associatedGenres.Distinct().ToList());
            foreach (var movie in moviesBeloningToGenres)
            {
                if (!storeMovies.Contains(movie) && !IsChristmasMovie(movie))
                {
                    storeMovies.Add(movie);
                }
            }

            var allMovies = await ParallelSelectAsync(storeMovies, ExternalApiConcurrency, movie => GetMovieById(movie.TmdbId.Value));

            allMovies = allMovies
                .Where(movie => duration.Contains("Any") ||
                                (duration.Contains("Short") && movie.Runtime <= 120) ||
                                (duration.Contains("Long") && movie.Runtime > 120))
                .Where(movie => openToForeignFilm || movie.OriginalLanguage == "en")
                .Where(movie => yearRange == "Anytime" ||
                                (yearRange == "After 2000" && movie.ReleaseDate.Value.Year >= 2000) ||
                                (yearRange == "Before 2000" && movie.ReleaseDate.Value.Year < 2000))
                .ToArray();

            var relevanceScores = CalculateRelevanceScores(allMovies.ToList(), associatedGenres);

            // Step 2: Add randomness to the relevance scores
            var adjustedScores = AddRandomness(relevanceScores);

            // Step 3: Sort the movies by the adjusted relevance score (descending)
            var topMovies = adjustedScores.OrderByDescending(pair => pair.Value)
                                          .Take(10)
                                          .Select(pair => pair.Key)
                                          .ToList();

            return topMovies;

        }

        public async Task<List<Movie>> GetFastRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange)
        {
            ChristmasListId = _listRepository.GetListByName("Christmas").ListId;

            List<string> feelingsFormmated = FormatFeelings(feelings);
            var storeMovies = new HashSet<EatMyMovies.DataAccess.Models.Movie>();

            foreach(var feeling in feelingsFormmated)
            {
                storeMovies.UnionWith(_rankingRepository.GetAllMoviesInList(feeling));
            }

            storeMovies.UnionWith(_movieRepository.GetMoviesOfGenres(feelingsFormmated));

            var filteredRecommendations = await ParallelSelectAsync(storeMovies, ExternalApiConcurrency, movie => GetMovieById(movie.TmdbId.Value));

            filteredRecommendations = filteredRecommendations
                .Where(movie => duration.Contains("Any") ||
                                (duration.Contains("Short") && movie.Runtime <= 120) ||
                                (duration.Contains("Long") && movie.Runtime > 120))
                .Where(movie => openToForeignFilm || movie.OriginalLanguage == "en")
                .Where(movie => yearRange == "Anytime" ||
                                (yearRange == "After 2000" && movie.ReleaseDate.Value.Year >= 2000) ||
                                (yearRange == "Before 2000" && movie.ReleaseDate.Value.Year < 2000))
                .ToArray();

            var shuffledReccys = ShuffleList<Movie>(filteredRecommendations);

            return shuffledReccys.ToList();
        }

        private List<string> FormatFeelings(string feelings)
        {
            List<string> formattedFeelings = feelings.Split(',').ToList();

            if (formattedFeelings.Contains("FeelGood"))
            {
                formattedFeelings.Remove("FeelGood");
                formattedFeelings.Add("Feel-good");
            }

            if (formattedFeelings.Contains("MindBending"))
            {
                formattedFeelings.Remove("MindBending");
                formattedFeelings.Add("Mind-bending");
            }

            if (formattedFeelings.Contains("Funny"))
            {
                formattedFeelings.Remove("Funny");
                formattedFeelings.Add("Comedies");
            }

            if (formattedFeelings.Contains("Scary"))
            {
                formattedFeelings.Remove("Scary");
                formattedFeelings.Add("Horrors");
            }
            return formattedFeelings;
        }

        private List<string> GetGenresLinkedToFeelings(string selectedFeelings)
        {
            var formattedFeelings = selectedFeelings.Split(',');

            var feelingToGenres = Mapper.GetFeelingToGenreMapping();

            return formattedFeelings
                .Select(feeling => (Feeling)Enum.Parse(typeof(Feeling), feeling))
                .Where(parsedFeeling => feelingToGenres.TryGetValue(parsedFeeling, out _))
                .SelectMany(parsedFeeling => feelingToGenres[parsedFeeling])
                .ToList();
        }

        private Dictionary<Movie, double> CalculateRelevanceScores(List<Movie> movies, List<string> selectedGenres)
        {
            var relevanceScores = new Dictionary<Movie, double>();

            foreach (var movie in movies)
            {
                // Count how many selected genres the movie matches
                var matchingGenres = movie.Genres.Select(x => x.Name).Intersect(selectedGenres).Count();

                // Calculate the relevance score (e.g., percentage of selected genres that match)
                double relevanceScore = (double)matchingGenres / selectedGenres.Count;

                // Assign the score to the movie
                relevanceScores[movie] = relevanceScore;
            }

            return relevanceScores;
        }

        private Dictionary<Movie, double> AddRandomness(Dictionary<Movie, double> relevanceScores)
        {
            var random = new Random();

            foreach (var movie in relevanceScores.Keys.ToList())
            {
                // Add a small random value to the relevance score
                double randomFactor = random.NextDouble() * 99; // Adjust 0.2 for more/less randomness

                relevanceScores[movie] += randomFactor;
            }

            return relevanceScores;
        }

        private bool IsChristmasMovie(EatMyMovies.DataAccess.Models.Movie movie)
        {
            return _rankingRepository.FilmExistsInList(movie.MovieId, ChristmasListId);
        }

        public IList<T> ShuffleList<T>(IList<T> list)
        {
            Random random = new Random();

            // Step 4: Implement the Fisher-Yates shuffle
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            return list;
        }

        private sealed class OmdbMovieResponse
        {
            [JsonPropertyName("imdbRating")]
            public string? ImdbRating { get; set; }
        }

        private sealed record RatingCacheValue(decimal? Rating);

        public async Task<Person> GetDirector(int movieId)
        {
            var credits = await GetCreditsSafely(movieId);
            return await BuildDirectorAsync(credits);
        }

        private async Task<Person> BuildDirectorAsync(Credits credits)
        {
            var tmdbDirector = credits.Crew?.FirstOrDefault(x => x.Job == "Director");
            if (tmdbDirector == null)
            {
                return new Person()
                {
                    Name = "Director unavailable",
                    Role = "Director",
                    ProfilePath = "/icon_only.png",
                    Biography = "Director information is not available for this film yet."
                };
            }

            var directorInfo = await GetPersonSafely(tmdbDirector.Id);

            Person director = new Person()
            {
                Id = tmdbDirector.Id,
                Name = tmdbDirector.Name,
                ProfilePath = tmdbDirector.ProfilePath != null
                    ? "https://image.tmdb.org/t/p/w185" + tmdbDirector.ProfilePath
                    : "/icon_only.png",
                Role = "Director",
                Biography = string.IsNullOrWhiteSpace(directorInfo?.Biography)
                    ? "Director biography is not available for this film yet."
                    : directorInfo.Biography
            };

            return director;
        }

        public async Task<List<Person>> GetActors(int movieId)
        {
            var credits = await GetCreditsSafely(movieId);
            return BuildActors(credits);
        }

        private static List<Person> BuildActors(Credits credits)
        {
            var tmdbActors = credits.Cast?.Where(x => x.KnownForDepartment == "Acting" && x.ProfilePath != null) ?? Enumerable.Empty<Cast>();

            List<Person> actors = new List<Person>();

            foreach(var actor in tmdbActors)
            {
                actors.Add(new Person()
                {
                    Id = actor.Id,
                    Name = actor.Name,
                    ProfilePath = "https://image.tmdb.org/t/p/w185" + actor.ProfilePath,
                    Role = "Actor",
                    Character = actor.Character

                });
            }

            return actors;
        }

        public List<MovieRanking> GetListRankingsForMovie(Guid movieId) {
            var rankingsFromStore = _rankingRepository.GetListRankingsForMovie(movieId);
            var rankings = new List<MovieRanking>();
            foreach(var storeRanking in rankingsFromStore)
            {
                rankings.Add(new MovieRanking()
                {
                    ListId = storeRanking.List.ListId,
                    ListName = storeRanking.List.Name,
                    MovieId = storeRanking.Movie.MovieId,
                    Ranking = storeRanking.Ranking,
                    ListRankingId = storeRanking.ListRankingId,

                });
            }

            return rankings;
        }

        public EatMyMovies.DataAccess.Models.Movie GetStoreMovieByTitle(string title)
        {
            var storeMovie = _movieRepository.GetMovieByTitle(title);
            return storeMovie;
        }

        public List<List> GetAllLists()
        {
            return _listRepository.GetAllLists();
        }

        private async Task<Movie> SearchTmdbMovieByTitleAsync(string title)
        {
            var searchResults = await ExecuteTmdbRequestAsync(
                () => _tmdbClient.SearchMovieAsync(title),
                $"searching for movie title '{title}'");
            var bestResult = searchResults.Results.FirstOrDefault();

            if (bestResult == null) throw new Exception("Movie not found");

            return await GetTmdbMovieByIdAsync(bestResult.Id);
        }

        private Task<Movie> GetTmdbMovieByIdAsync(int id)
        {
            return ExecuteTmdbRequestAsync(
                () => _tmdbClient.GetMovieAsync(id),
                $"getting movie {id}");
        }

        private async Task<Video?> GetTmdbTrailerAsync(int movieId)
        {
            var videos = await ExecuteTmdbRequestAsync(
                () => _tmdbClient.GetMovieVideosAsync(movieId),
                $"getting videos for movie {movieId}");
            return videos.Results?.FirstOrDefault(v => v.Type == "Trailer");
        }

        private Task<Credits> GetTmdbCreditsAsync(int movieId)
        {
            return ExecuteTmdbRequestAsync(
                () => _tmdbClient.GetMovieCreditsAsync(movieId),
                $"getting credits for movie {movieId}");
        }

        private Task<TmdbPerson?> GetTmdbPersonAsync(int personId)
        {
            return ExecuteTmdbRequestAsync(
                () => _tmdbClient.GetPersonAsync(personId),
                $"getting person {personId}");
        }

        private async Task<Credits> GetCreditsSafely(int movieId)
        {
            var cacheKey = $"tmdb:credits:{movieId}";

            if (!_cache.TryGetValue(cacheKey, out Credits credits))
            {
                try
                {
                    credits = await _getCredits(movieId);
                    _cache.Set(cacheKey, credits, TimeSpan.FromHours(6));
                }
                catch
                {
                    credits = new Credits
                    {
                        Cast = new List<Cast>(),
                        Crew = new List<Crew>()
                    };
                    _cache.Set(cacheKey, credits, TimeSpan.FromMinutes(15));
                }
            }

            return credits;
        }

        private async Task<TmdbPerson?> GetPersonSafely(int personId)
        {
            try
            {
                return await _getPerson(personId);
            }
            catch
            {
                return null;
            }
        }

        private static string GetMovieByIdCacheKey(int id)
        {
            return $"tmdb:movie:{id}";
        }

        private static string NormalizeTitleForCache(string title)
        {
            return title.Trim().ToUpperInvariant();
        }

        private static async Task<TResult[]> ParallelSelectAsync<TSource, TResult>(
            IEnumerable<TSource> source,
            int maxConcurrency,
            Func<TSource, Task<TResult>> selector)
        {
            var items = source.ToList();
            var results = new TResult[items.Count];
            using var throttler = new SemaphoreSlim(maxConcurrency);

            var tasks = items.Select(async (item, index) =>
            {
                await throttler.WaitAsync();
                try
                {
                    results[index] = await selector(item);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }

        private static async Task<T> ExecuteTmdbRequestAsync<T>(Func<Task<T>> request, string operation)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await request();
                }
                catch (Exception ex) when (attempt < TmdbMaxRetryAttempts && IsTransientTmdbException(ex))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                }
                catch (Exception ex) when (IsTransientTmdbException(ex))
                {
                    throw new HttpRequestException(
                        $"TMDb request failed while {operation} after {TmdbMaxRetryAttempts} attempts.",
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

    }
}
