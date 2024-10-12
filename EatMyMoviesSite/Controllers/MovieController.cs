using EatMyMovies.DataAccess.Models;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using EatMyMoviesSite.Models;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{

    [Route("movie")]
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


        [Route("detail")]
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
                var rating = await _movieService.GetImdbRating(movie.Title);
                var movieDetail = Mapper.MapToMovieDetail(movie, trailer, rating);
                return View(movieDetail);
            }
            catch (Exception ex)
            {
                return View();
            }
        }


        [Route("search")]
        public async Task<IActionResult> Search()
        {
            return View();
        }

        public async Task<List<MovieDropdown>> SearchForMovie(string titleSearch)
        {
            var results = await _movieService.SearchMoviesByTitle(titleSearch);
            return results;
        }


        [Route("recommender")]
        public IActionResult Recommender()
		{
			return View();
		}

        [HttpGet("GetGenres")]
        public List<string> GetGenres()
        {
            var genres = _movieService.GetAllGenres();
            var shuffledGenres = _movieService.ShuffleList<Genre>(genres);
            return genres.Select(x => x.Name).ToList();
        }

        [HttpGet("GetFeelings")]
        public List<string> GetFeelings()
        {
            var feelings = Enum.GetNames(typeof(Feeling)).ToList();
            return feelings;
        }

        [HttpGet("GetRecommendations")]
        public async Task<List<MovieDetail>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange)
        {
            var recommendations = await _movieService.GetRecommendations(feelings, duration, openToForeignFilm, yearRange);

            var movieDetailTasks = recommendations.Select(async movie =>
            {
                // Run the async operations in parallel
                var trailerTask = _movieService.GetTrailer(movie.Id);
                var ratingTask = _movieService.GetImdbRating(movie.Title);

                // Await both tasks to complete
                var trailer = await trailerTask;
                var rating = await ratingTask;

                // Map to MovieDetail
                return Mapper.MapToMovieDetail(movie, trailer, rating);
            });

            // Wait for all tasks to complete
            var movieDetails = await Task.WhenAll(movieDetailTasks);

            return movieDetails.ToList();
        }

    }
}
