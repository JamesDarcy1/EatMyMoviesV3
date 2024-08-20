using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite.Controllers
{
	public class ListController : Controller
    {
        private readonly IMovieService _movieService;

        public ListController(IMovieService movieService)
        {
            _movieService = movieService;
		}

        public async Task<IActionResult> Top100(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Top 100", page);
            return View("~/Views/List/List.cshtml", list);
        }

        public async Task<IActionResult> Comedies(int page = 1)
        {
			var list = await _movieService.BuildMovieList("Comedies", page);
			return View("~/Views/List/List.cshtml", list);
		}

		public async Task<IActionResult> ForeignFilms(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Foreign Films", page);
			return View("~/Views/List/List.cshtml", list);
		}

		public async Task<IActionResult> Documentaries(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Documentaries", page);
			return View("~/Views/List/List.cshtml", list);
		}

		public async Task<IActionResult> Christmas(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Christmas", page);
			return View("~/Views/List/List.cshtml", list);
		}

        public async Task<IActionResult> StandoutSoundtracks(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Standout Soundtracks", page);
            return View("~/Views/List/List.cshtml", list);
        }

        public async Task<IActionResult> Iconic80s(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Iconic 80s", page);
            return View("~/Views/List/List.cshtml", list);
        }
    }
}
