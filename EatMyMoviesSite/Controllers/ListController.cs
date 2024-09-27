using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{
    [Route("list")]
	public class ListController : Controller
    {
        private readonly IMovieService _movieService;

        public ListController(IMovieService movieService)
        {
            _movieService = movieService;
		}


        [Route("top-100")]
        public async Task<IActionResult> Top100(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Top 100", page);
            return View("~/Views/list/list.cshtml", list);
        }


        [Route("comdies")]
        public async Task<IActionResult> Comedies(int page = 1)
        {
			var list = await _movieService.BuildMovieList("Comedies", page);
			return View("~/Views/List/List.cshtml", list);
		}

        [Route("foreign-films")]
        public async Task<IActionResult> ForeignFilms(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Foreign Films", page);
			return View("~/Views/List/List.cshtml", list);
		}


        [Route("documentaries")]
        public async Task<IActionResult> Documentaries(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Documentaries", page);
			return View("~/Views/List/List.cshtml", list);
		}

        [Route("christmas")]
        public async Task<IActionResult> Christmas(int page = 1)
		{
			var list = await _movieService.BuildMovieList("Christmas", page);
			return View("~/Views/List/List.cshtml", list);
		}


        [Route("standout-soundtracks")]
        public async Task<IActionResult> StandoutSoundtracks(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Standout Soundtracks", page);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("iconic-80s")]
        public async Task<IActionResult> Iconic80s(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Iconic 80s", page);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("disney")]
        public async Task<IActionResult> Disney(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Disney", page);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("horrors")]
        public async Task<IActionResult> Horrors(int page = 1)
        {
            var list = await _movieService.BuildMovieList("Horrors", page);
            return View("~/Views/List/List.cshtml", list);
        }
    }
}
