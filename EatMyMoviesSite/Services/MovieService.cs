using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using OMDbSharp;
using TMDbLib.Client;
using TMDbLib.Objects.General;
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

		public MovieService(IRankingRepository rankingRepository, IConfiguration configuration, IListRepository listRepository, IMovieRepository movieRepository)
		{
			_isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
			_tmdbClient = new TMDbClient(configuration["Tmdb:ApiKey"]);
			_omdbClient = new OMDbClient(configuration["Omdb:ApiKey"], false);
			_rankingRepository = rankingRepository;
			_listRepository = listRepository;
			_movieRepository = movieRepository;
		}

		public async Task<Movie> GetMovieByTitle(string title)
		{
			var searchResults = await _tmdbClient.SearchMovieAsync(title);
			var bestResult = searchResults.Results.FirstOrDefault();

			if (bestResult == null) throw new Exception("Movie not found");

			var movie = await _tmdbClient.GetMovieAsync(bestResult.Id);
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
			var movie = await _tmdbClient.GetMovieAsync(id);
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
			var movie = await _omdbClient.GetItemByTitle(movieTitle);
			if (movie.IMDbRating != null)
			{
				var imdbRating = Decimal.Parse(movie?.IMDbRating);
                return imdbRating;
            }
			return null;
        }

		public async Task<MovieList> BuildMovieList(string listTitle, int page)
		{
			try
			{
				var list = _listRepository.GetListByName(listTitle);
				var moviesList = new MovieList() { Name = list.Name, Description = list.Description, Movies = new List<ListMovie>() };
				var storedMovies = _rankingRepository.GetMoviesForList(listTitle);
				var totalMovies = storedMovies.Count();
				var totalPages = (int)Math.Ceiling((double)totalMovies / _moviesPerPage);
				page = Math.Max(1, Math.Min(page, totalPages));

				moviesList.TotalPages = totalPages;
				moviesList.CurrentPage = page;

				var moviesForPage = storedMovies 
					.Skip((page - 1) * _moviesPerPage)
					.Take(_moviesPerPage)
					.ToList();

				foreach (var movie in moviesForPage)
				{
					var ranking = _rankingRepository.GetRankingOfMovie(movie.MovieId, listTitle);
					var imdbRating = await GetImdbRating(movie.Title);
					//if (!_isDevelopment)
					//{
						var tmdbMovie = await GetMoviesById(movie.TmdbId.Value);
						var mappedMovie = Mapper.BuildListMovie(tmdbMovie, imdbRating, ranking);
						moviesList.Movies.Add(mappedMovie);
					//}
					//else
					//{
					//	moviesList.Movies.Add(new ListMovie() { Title = movie.Title, Ranking = ranking, ImdbRating = imdbRating });
					//}
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

		public EatMyMovies.DataAccess.Models.Movie GetRecommendationByGenre(string genre)
		{
			var moviesOfGenre = _movieRepository.GetMoviesByGenre(genre);
			Random random = new Random();
			var randomMovieIndex = random.Next(moviesOfGenre.Count);
			var recommendation = moviesOfGenre.ElementAt(randomMovieIndex);
			return recommendation;
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
