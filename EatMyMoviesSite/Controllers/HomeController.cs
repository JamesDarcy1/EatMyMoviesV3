using EatMyMoviesSite.Models;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EatMyMoviesSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieService _movieService;

        public HomeController(ILogger<HomeController> logger, IMovieService movieService)
        {
            _logger = logger;
            _movieService = movieService;
        }

        public async Task<IActionResult> Index()
        {
            var movieOfTheWeek = "Cloudy with a chance of meatballs";
            var tmdbMovie = await _movieService.GetMovieByTitle(movieOfTheWeek);
            var imdbRating = await _movieService.GetImdbRating(tmdbMovie.Title);

            var summary = Mapper.MapToMovieSummary(tmdbMovie, imdbRating);

            return View(summary);
        }


        [Route("about")]
        public IActionResult About()
        {
            return View();
        }


        [Route("contact")]
        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}