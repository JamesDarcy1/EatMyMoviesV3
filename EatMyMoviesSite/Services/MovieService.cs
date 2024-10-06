using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using Microsoft.Extensions.Caching.Memory;
using OMDbSharp;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using Movie = TMDbLib.Objects.Movies.Movie;

namespace EatMyMoviesSite.Services
{
    public class MovieService : IMovieService
    {
        private readonly TMDbClient _tmdbClient;
        private readonly OMDbClient _omdbClient;
        private readonly IRankingRepository _rankingRepository;
        private readonly IListRepository _listRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly int _moviesPerPage = 10;
        private readonly bool _isDevelopment;
        private Guid ChristmasListId;
        private readonly IMemoryCache _cache;

        public MovieService(IRankingRepository rankingRepository,
                            IConfiguration configuration,
                            IListRepository listRepository,
                            IMovieRepository movieRepository,
                            IMemoryCache memoryCache)
        {
            _isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
            _tmdbClient = new TMDbClient(configuration["Tmdb:ApiKey"]);
            _omdbClient = new OMDbClient(configuration["Omdb:ApiKey"], false);
            _rankingRepository = rankingRepository;
            _listRepository = listRepository;
            _movieRepository = movieRepository;
            _cache = memoryCache;
        }

        public async Task<Movie> GetMovieByTitle(string title)
        {
            var cacheKey = $"Movie_{title}";

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                var searchResults = await _tmdbClient.SearchMovieAsync(title);
                var bestResult = searchResults.Results.FirstOrDefault();

                if (bestResult == null) throw new Exception("Movie not found");

                movie = await _tmdbClient.GetMovieAsync(bestResult.Id);

                _cache.Set(cacheKey, movie, TimeSpan.FromHours(6));
            }

            return movie;
        }

        public async Task<List<MovieDropdown>> SearchMoviesByTitle(string titleSearch)
        {
            var searchResults = await _tmdbClient.SearchMovieAsync(titleSearch, 1);
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

        public async Task<Movie> GetMoviesById(int id)
        {
            var cacheKey = $"Movie_{id}";

            if (!_cache.TryGetValue(cacheKey, out Movie movie))
            {
                movie = await _tmdbClient.GetMovieAsync(id);
                _cache.Set(cacheKey, movie, TimeSpan.FromHours(6)); 
            }
            return movie;
        }

        public async Task<Video> GetTrailer(int movieId)
        {
            var videos = await _tmdbClient.GetMovieVideosAsync(movieId);
            var trailer = videos.Results.FirstOrDefault(v => v.Type == "Trailer");
            return trailer;
        }

        public async Task<decimal?> GetImdbRating(string movieTitle)
        {
            var cacheKey = $"Rating_{movieTitle}";

            if (!_cache.TryGetValue(cacheKey, out decimal? rating)) {
                var movie = await _omdbClient.GetItemByTitle(movieTitle);
                if (movie?.IMDbRating != null)
                {
                    var imdbRating = Decimal.Parse(movie?.IMDbRating);
                    if(imdbRating == null) {
                        rating = null;
                    } else
                    {
                        rating = imdbRating;
                    }
                }
                _cache.Set(cacheKey, rating, TimeSpan.FromHours(6));

            }
            return rating;
        }

        public async Task<MovieList> BuildMovieList(string listTitle, int page)
        {
            try
            {
                var list = _listRepository.GetListByName(listTitle);
                var moviesList = new MovieList() { Name = list.Name, Description = list.Description, Movies = new List<ListMovie>() };
                var moviesForPage =  _rankingRepository.GetMoviesForListByPage(listTitle, page).ToList();
                var totalMovies = _rankingRepository.GetListCount(listTitle);
                var totalPages = (int)Math.Ceiling((double)totalMovies / _moviesPerPage);
                page = Math.Max(1, Math.Min(page, totalPages));

                moviesList.TotalPages = totalPages;
                moviesList.CurrentPage = page;

                foreach (var movie in moviesForPage)
                {
                    var ranking = _rankingRepository.GetRankingOfMovie(movie.MovieId, listTitle);
                    var imdbRating = await GetImdbRating(movie.Title);
                    var tmdbMovie = await GetMoviesById(movie.TmdbId.Value);
                    var mappedMovie = Mapper.BuildListMovie(tmdbMovie, imdbRating, ranking);
                    moviesList.Movies.Add(mappedMovie);
                }

                moviesList.Movies = moviesList.Movies.OrderBy(x => x.Ranking).ToList();
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

            var allMovies = await Task.WhenAll(storeMovies.Select(movie => _tmdbClient.GetMovieAsync(movie.TmdbId.Value)));

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
                double randomFactor = random.NextDouble() * 0.2; // Adjust 0.2 for more/less randomness

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
    }
}
