using EatMyMovies.DataAccess.Models;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Models;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Detail(string title, int? tmdbId = null)
        {
            try
            {
                TMDbLib.Objects.Movies.Movie movie;
                if(tmdbId != null)
                {
                    movie = await _movieService.GetMoviesById(tmdbId.Value);
                }
                else { 
                    movie = await _movieService.GetMovieByTitle(title);
                }
                var trailer = await _movieService.GetTrailer(movie.Id);
                var rating = await _movieService.GetImdbRating(title);
                var movieDetail = Mapper.MapToMovieDetail(movie, trailer, rating);
                return View(movieDetail);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public async Task<IActionResult> Search()
        {
            return View();
        }

        public async Task<List<MovieDropdown>> SearchForMovie(string titleSearch)
        {
            var results = await _movieService.SearchMoviesByTitle(titleSearch);
            return results;
        }

        public IActionResult Recommender()
		{
			return View();
		}

        public List<string> GetGenres()
        {
            var genres = _movieService.GetAllGenres();
            var shuffledGenres = _movieService.ShuffleList<Genre>(genres);
            return genres.Select(x => x.Name).ToList();
        }

        public async Task<List<MovieDetail>> GetRecommendationsByGenre(string genre)
		{
			var movies = _movieService.GetRecommendationsByGenre(genre);
            var recommendations = new List<MovieDetail>();
            foreach (var movie in movies)
            {
                var tmdbMovie = await _movieService.GetMovieByTitle(movie.Title);
                var trailer = await _movieService.GetTrailer(tmdbMovie.Id);
                var rating = await _movieService.GetImdbRating(movie.Title);
                var movieDetail = Mapper.MapToMovieDetail(tmdbMovie, trailer, rating);
                recommendations.Add(movieDetail);
            }
			return recommendations;
		}

	}
}
