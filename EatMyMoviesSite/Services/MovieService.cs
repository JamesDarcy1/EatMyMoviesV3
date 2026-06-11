using EatMyMovies.DataAccess.QueryModels;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using EatMyMoviesSite.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using DataGenre = EatMyMovies.DataAccess.Models.Genre;
using DataList = EatMyMovies.DataAccess.Models.List;
using DataMovie = EatMyMovies.DataAccess.Models.Movie;
using Movie = TMDbLib.Objects.Movies.Movie;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMoviesSite.Services
{
    internal class MovieService : IMovieService
    {
        private readonly ITmdbMovieClient _tmdbClient;
        private readonly IOmdbClient _omdbClient;
        private readonly MovieExternalApiOptions _externalApiOptions;
        private readonly IRankingRepository _rankingRepository;
        private readonly IListRepository _listRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly int _moviesPerPage = 10;
        private readonly IMemoryCache _cache;

        public MovieService(IRankingRepository rankingRepository,
                            IListRepository listRepository,
                            IMovieRepository movieRepository,
                            IMemoryCache memoryCache,
                            ITmdbMovieClient tmdbClient,
                            IOmdbClient omdbClient,
                            IOptions<MovieExternalApiOptions> externalApiOptions)
        {
            _rankingRepository = rankingRepository;
            _listRepository = listRepository;
            _movieRepository = movieRepository;
            _cache = memoryCache;
            _tmdbClient = tmdbClient;
            _omdbClient = omdbClient;
            _externalApiOptions = externalApiOptions.Value;
        }

        public async Task<Movie> GetMovieByTitle(string title)
        {
            var cacheKey = $"tmdb:movie:title:{NormalizeTitleForCache(title)}";

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                movie = await SearchTmdbMovieByTitleAsync(title);
                _cache.Set(cacheKey, movie, _externalApiOptions.MovieCacheDuration);
                _cache.Set(GetMovieByIdCacheKey(movie.Id), movie, _externalApiOptions.MovieCacheDuration);
            }

            return movie;
        }

        public async Task<List<MovieDropdown>> SearchMoviesByTitle(string titleSearch)
        {
            var searchResults = await _tmdbClient.SearchMoviesAsync(titleSearch, 1);
            var reducedList = searchResults.Results.Where(x => x.PosterPath != null)
                                                    .Select(x =>
                                                        new MovieDropdown()
                                                        {
                                                            Id = x.Id,
                                                            Title = x.Title,
                                                            PosterPath = x.PosterPath
                                                        }).Take(_externalApiOptions.SearchDropdownLimit).ToList();
            return reducedList;
        }

        public async Task<Movie> GetMovieById(int id)
        {
            var cacheKey = GetMovieByIdCacheKey(id);

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                movie = await _tmdbClient.GetMovieByIdAsync(id);
                _cache.Set(cacheKey, movie, _externalApiOptions.MovieCacheDuration); 
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
                    trailer = await GetTmdbTrailerAsync(movieId);
                    _cache.Set(cacheKey, trailer, _externalApiOptions.TrailerCacheDuration);
                }
                catch
                {
                    trailer = null;
                    _cache.Set(cacheKey, trailer, _externalApiOptions.TrailerFailureCacheDuration);
                }
            }

            return trailer;
        }

        public async Task<decimal?> GetImdbRating(string movieTitle)
        {
            var cacheKey = $"omdb:rating:{NormalizeTitleForCache(movieTitle)}";

            if (!_cache.TryGetValue(cacheKey, out RatingCacheValue? cachedRating)) {
                var rating = await _omdbClient.GetImdbRatingAsync(movieTitle);
                var cacheDuration = rating.HasValue
                    ? _externalApiOptions.ImdbRatingCacheDuration
                    : _externalApiOptions.UnknownImdbRatingCacheDuration;

                cachedRating = new RatingCacheValue(rating);
                _cache.Set(cacheKey, cachedRating, cacheDuration);
            }
            return cachedRating.Rating;
        }

        public async Task<MovieDetail> BuildMovieDetail(string? title, int? tmdbId, bool includeListContext, CancellationToken cancellationToken = default)
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
                movieDetail.Lists = await GetAllListsAsync(cancellationToken);

                var storeMovie = await GetStoreMovieByTitleAsync(movie.Title, cancellationToken);
                if (storeMovie != null)
                {
                    movieDetail.Rankings = await GetListRankingsForMovieAsync(storeMovie.MovieId, cancellationToken);
                }
            }

            return movieDetail;
        }

        public async Task<MovieList> BuildMovieList(string listTitle, int page, CancellationToken cancellationToken = default)
        {
            var list = await _listRepository.GetListByNameAsync(listTitle, cancellationToken);
            if (list is null)
            {
                throw new Exception($"List '{listTitle}' was not found.");
            }

            var moviesList = new MovieList()
            {
                Name = list.Name,
                Description = list.Description,
                Movies = new List<ListMovie>()
            };

            var totalMovies = await _rankingRepository.GetListCountAsync(listTitle, cancellationToken);
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalMovies / _moviesPerPage));
            page = Math.Max(1, Math.Min(page, totalPages));
            var moviesForPage = await _rankingRepository.GetMoviesForListByPageAsync(listTitle, page, _moviesPerPage, cancellationToken);

            moviesList.TotalPages = totalPages;
            moviesList.CurrentPage = page;

            var results = await ParallelSelectAsync(moviesForPage, _externalApiOptions.ExternalApiConcurrency, async movie =>
            {
                if (!movie.TmdbId.HasValue)
                {
                    throw new Exception($"Movie '{movie.Title}' does not have a TMDb id.");
                }

                var imdbTask = GetImdbRating(movie.Title);
                var tmdbTask = GetMovieById(movie.TmdbId.Value);

                var tmdbMovie = await tmdbTask;
                var imdbRating = await imdbTask;

                return Mapper.BuildListMovie(tmdbMovie, imdbRating, movie.Ranking);
            });

            moviesList.Movies.AddRange(results);

            return moviesList;
        }

        public Task<List<DataGenre>> GetAllGenresAsync(CancellationToken cancellationToken = default)
        {
            return _movieRepository.GetAllGenresAsync(cancellationToken);
        }

        public async Task<List<Movie>> GetRecommendations(
            string feelings,
            string duration,
            bool openToForeignFilm,
            string yearRange,
            CancellationToken cancellationToken = default)
        {
            var christmasMovies = await _rankingRepository.GetMovieSummariesInListAsync("Christmas", cancellationToken);
            var christmasMovieIds = christmasMovies.Select(movie => movie.MovieId).ToHashSet();

            var associatedGenres = GetGenresLinkedToFeelings(feelings);
            var moviesBelongingToGenres = await _movieRepository.GetMoviesOfGenresAsync(associatedGenres.Distinct().ToList(), cancellationToken);
            var storeMovies = DistinctMoviesById(moviesBelongingToGenres)
                .Where(movie => !christmasMovieIds.Contains(movie.MovieId) && movie.TmdbId.HasValue)
                .ToList();

            var allMovies = await ParallelSelectAsync(storeMovies, _externalApiOptions.ExternalApiConcurrency, movie => GetMovieById(movie.TmdbId!.Value));

            allMovies = FilterRecommendations(allMovies, duration, openToForeignFilm, yearRange);

            var relevanceScores = CalculateRelevanceScores(allMovies.ToList(), associatedGenres);
            var adjustedScores = AddRandomness(relevanceScores);

            return adjustedScores.OrderByDescending(pair => pair.Value)
                                 .Take(10)
                                 .Select(pair => pair.Key)
                                 .ToList();
        }

        public async Task<List<Movie>> GetFastRecommendations(
            string feelings,
            string duration,
            bool openToForeignFilm,
            string yearRange,
            CancellationToken cancellationToken = default)
        {
            var formattedFeelings = FormatFeelings(feelings);
            var storeMovies = new List<StoredMovieSummary>();

            foreach(var feeling in formattedFeelings)
            {
                storeMovies.AddRange(await _rankingRepository.GetMovieSummariesInListAsync(feeling, cancellationToken));
            }

            storeMovies.AddRange(await _movieRepository.GetMoviesOfGenresAsync(formattedFeelings, cancellationToken));

            var distinctMovies = DistinctMoviesById(storeMovies)
                .Where(movie => movie.TmdbId.HasValue)
                .ToList();

            var filteredRecommendations = await ParallelSelectAsync(distinctMovies, _externalApiOptions.ExternalApiConcurrency, movie => GetMovieById(movie.TmdbId!.Value));
            filteredRecommendations = FilterRecommendations(filteredRecommendations, duration, openToForeignFilm, yearRange);

            var shuffledReccys = ShuffleList<Movie>(filteredRecommendations);

            return shuffledReccys.ToList();
        }

        private static Movie[] FilterRecommendations(IEnumerable<Movie> movies, string duration, bool openToForeignFilm, string yearRange)
        {
            return movies
                .Where(movie => duration.Contains("Any") ||
                                (duration.Contains("Short") && movie.Runtime <= 120) ||
                                (duration.Contains("Long") && movie.Runtime > 120))
                .Where(movie => openToForeignFilm || movie.OriginalLanguage == "en")
                .Where(movie => yearRange == "Anytime" ||
                                (yearRange == "After 2000" && movie.ReleaseDate.Value.Year >= 2000) ||
                                (yearRange == "Before 2000" && movie.ReleaseDate.Value.Year < 2000))
                .ToArray();
        }

        private static List<StoredMovieSummary> DistinctMoviesById(IEnumerable<StoredMovieSummary> movies)
        {
            return movies
                .GroupBy(movie => movie.MovieId)
                .Select(group => group.First())
                .ToList();
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
                var matchingGenres = movie.Genres.Select(x => x.Name).Intersect(selectedGenres).Count();
                var relevanceScore = (double)matchingGenres / selectedGenres.Count;
                relevanceScores[movie] = relevanceScore;
            }

            return relevanceScores;
        }

        private Dictionary<Movie, double> AddRandomness(Dictionary<Movie, double> relevanceScores)
        {
            var random = new Random();

            foreach (var movie in relevanceScores.Keys.ToList())
            {
                double randomFactor = random.NextDouble() * 99;
                relevanceScores[movie] += randomFactor;
            }

            return relevanceScores;
        }

        public IList<T> ShuffleList<T>(IList<T> list)
        {
            Random random = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            return list;
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

        public async Task<List<MovieRanking>> GetListRankingsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default) {
            var rankingsFromStore = await _rankingRepository.GetListRankingsForMovieAsync(movieId, cancellationToken);
            var rankings = new List<MovieRanking>();
            foreach(var storeRanking in rankingsFromStore)
            {
                rankings.Add(new MovieRanking()
                {
                    ListId = storeRanking.ListId,
                    ListName = storeRanking.ListName,
                    MovieId = storeRanking.MovieId,
                    Ranking = storeRanking.Ranking,
                    ListRankingId = storeRanking.ListRankingId,
                });
            }

            return rankings;
        }

        public Task<DataMovie?> GetStoreMovieByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            return _movieRepository.GetMovieByTitleAsync(title, cancellationToken);
        }

        public Task<List<DataList>> GetAllListsAsync(CancellationToken cancellationToken = default)
        {
            return _listRepository.GetAllListsAsync(cancellationToken);
        }

        private async Task<Movie> SearchTmdbMovieByTitleAsync(string title)
        {
            var searchResults = await _tmdbClient.SearchMoviesAsync(title);
            var bestResult = searchResults.Results.FirstOrDefault();

            if (bestResult == null) throw new Exception("Movie not found");

            return await _tmdbClient.GetMovieByIdAsync(bestResult.Id);
        }

        private async Task<Video?> GetTmdbTrailerAsync(int movieId)
        {
            var videos = await _tmdbClient.GetMovieVideosAsync(movieId);
            return videos.Results?.FirstOrDefault(v => v.Type == "Trailer");
        }

        private async Task<Credits> GetCreditsSafely(int movieId)
        {
            var cacheKey = $"tmdb:credits:{movieId}";

            if (!_cache.TryGetValue(cacheKey, out Credits credits))
            {
                try
                {
                    credits = await _tmdbClient.GetMovieCreditsAsync(movieId);
                    _cache.Set(cacheKey, credits, _externalApiOptions.CreditsCacheDuration);
                }
                catch
                {
                    credits = new Credits
                    {
                        Cast = new List<Cast>(),
                        Crew = new List<Crew>()
                    };
                    _cache.Set(cacheKey, credits, _externalApiOptions.CreditsFailureCacheDuration);
                }
            }

            return credits;
        }

        private async Task<TmdbPerson?> GetPersonSafely(int personId)
        {
            try
            {
                return await _tmdbClient.GetPersonAsync(personId);
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
    }
}
