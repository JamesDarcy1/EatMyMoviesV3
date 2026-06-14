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
        public async Task<IActionResult> Top100(int page = 1, CancellationToken cancellationToken = default)
        {
            var list = await _movieService.BuildMovieList("Top 100", page, cancellationToken);
            return View("~/Views/list/list.cshtml", list);
        }

        [HttpGet("top100")]
        public IActionResult LegacyTop100(int page = 1)
        {
            return RedirectToActionPermanent(nameof(Top100), new { page });
        }


        [Route("comedies")]
        public async Task<IActionResult> Comedies(int page = 1, CancellationToken cancellationToken = default)
        {
			var list = await _movieService.BuildMovieList("Comedies", page, cancellationToken);
			return View("~/Views/List/List.cshtml", list);
		}

        [Route("foreign-films")]
        public async Task<IActionResult> ForeignFilms(int page = 1, CancellationToken cancellationToken = default)
		{
			var list = await _movieService.BuildMovieList("Foreign Films", page, cancellationToken);
			return View("~/Views/List/List.cshtml", list);
		}


        [Route("documentaries")]
        public async Task<IActionResult> Documentaries(int page = 1, CancellationToken cancellationToken = default)
		{
			var list = await _movieService.BuildMovieList("Documentaries", page, cancellationToken);
			return View("~/Views/List/List.cshtml", list);
		}

        [Route("christmas")]
        public async Task<IActionResult> Christmas(int page = 1, CancellationToken cancellationToken = default)
		{
			var list = await _movieService.BuildMovieList("Christmas", page, cancellationToken);
			return View("~/Views/List/List.cshtml", list);
		}


        [Route("standout-soundtracks")]
        public async Task<IActionResult> StandoutSoundtracks(int page = 1, CancellationToken cancellationToken = default)
        {
            var list = await _movieService.BuildMovieList("Standout Soundtracks", page, cancellationToken);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("iconic-80s")]
        public async Task<IActionResult> Iconic80s(int page = 1, CancellationToken cancellationToken = default)
        {
            var list = await _movieService.BuildMovieList("Iconic 80s", page, cancellationToken);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("disney")]
        public async Task<IActionResult> Disney(int page = 1, CancellationToken cancellationToken = default)
        {
            var list = await _movieService.BuildMovieList("Disney", page, cancellationToken);
            return View("~/Views/List/List.cshtml", list);
        }


        [Route("horrors")]
        public async Task<IActionResult> Horrors(int page = 1, CancellationToken cancellationToken = default)
        {
            var list = await _movieService.BuildMovieList("Horrors", page, cancellationToken);
            return View("~/Views/List/List.cshtml", list);
        }
    }
}
