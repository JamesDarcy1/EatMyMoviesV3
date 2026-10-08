using EatMyMoviesSite.DTOs;
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

        [HttpGet("/")]
        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel
            {
                MovieOfTheWeek = await _movieService.BuildMovieOfTheWeekAsync()
            };

            return View(model);
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

        [HttpGet("privacy")]
        public IActionResult Privacy() => View();

        [Route("error/{statusCode:int}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statusCode = 500)
        {
            statusCode = statusCode is >= 400 and <= 599 ? statusCode : 404;
            Response.StatusCode = statusCode;
            return View(new ErrorViewModel
            {
                StatusCode = statusCode,
                RequestId = statusCode == 500 ? Activity.Current?.Id ?? HttpContext.TraceIdentifier : null
            });
        }
    }
}
