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

        public async Task<IActionResult> Detail(string title)
        {
            try
            {
                var movie = await _movieService.GetMovieByTitle(title);
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
            //var results = await _movieService.GetMovieByTitle(title);
            return View();
        }

		public IActionResult Recommender()
		{
			return View();
		}

		[HttpGet]
		public IActionResult GetGenres()
		{
			var genres = new List<string> { "Action", "Comedy", "Drama", "Horror", "Romance" };
			return Json(genres);
		}

		[HttpGet]
		public async Task<IActionResult> GetRecommendation(string genre)
		{
            var title = "Die Hard";
			var movie = await _movieService.GetMovieByTitle(title);
			var trailer = await _movieService.GetTrailer(movie.Id);
			var rating = await _movieService.GetImdbRating(title);
			var movieDetail = Mapper.MapToMovieDetail(movie, trailer, rating);
			return Json(movieDetail);
		}
	}
}
