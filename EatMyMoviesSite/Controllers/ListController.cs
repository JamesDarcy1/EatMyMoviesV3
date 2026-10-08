using EatMyMoviesSite.Services;
using EatMyMoviesSite.Models;
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

        [HttpGet("")]
        public IActionResult Index() => View(SeoMetadataService.Lists.Values
            .Select(list => new ListDirectoryEntry(list.Path, list.Title))
            .ToList());


        [Route("top-100")]
        public async Task<IActionResult> Top100(int page = 1, CancellationToken cancellationToken = default)
        {
            return await BuildList("Top 100", "~/Views/list/list.cshtml", page, cancellationToken);
        }

        private async Task<IActionResult> BuildList(string name, string view, int page, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || page < 1) return NotFound();
            var list = await _movieService.BuildMovieList(name, page, cancellationToken);
            if (page == 1 && ControllerContext.HttpContext?.Request.Query.ContainsKey("page") == true)
                return RedirectPermanent(Request.PathBase + Request.Path);
            return View(view, list);
        }
        [HttpGet("top100")]
        public IActionResult LegacyTop100(int page = 1)
        {
            if (!ModelState.IsValid || page < 1) return NotFound();
            return RedirectToActionPermanent(nameof(Top100), page == 1 ? null : new { page });
        }


        [Route("comedies")]
        public async Task<IActionResult> Comedies(int page = 1, CancellationToken cancellationToken = default)
        {
			return await BuildList("Comedies", "~/Views/List/List.cshtml", page, cancellationToken);
		}

        [Route("foreign-films")]
        public async Task<IActionResult> ForeignFilms(int page = 1, CancellationToken cancellationToken = default)
		{
			return await BuildList("Foreign Films", "~/Views/List/List.cshtml", page, cancellationToken);
		}


        [Route("documentaries")]
        public async Task<IActionResult> Documentaries(int page = 1, CancellationToken cancellationToken = default)
		{
			return await BuildList("Documentaries", "~/Views/List/List.cshtml", page, cancellationToken);
		}

        [Route("christmas")]
        public async Task<IActionResult> Christmas(int page = 1, CancellationToken cancellationToken = default)
		{
			return await BuildList("Christmas", "~/Views/List/List.cshtml", page, cancellationToken);
		}


        [Route("standout-soundtracks")]
        public async Task<IActionResult> StandoutSoundtracks(int page = 1, CancellationToken cancellationToken = default)
        {
            return await BuildList("Standout Soundtracks", "~/Views/List/List.cshtml", page, cancellationToken);
        }


        [Route("iconic-80s")]
        public async Task<IActionResult> Iconic80s(int page = 1, CancellationToken cancellationToken = default)
        {
            return await BuildList("Iconic 80s", "~/Views/List/List.cshtml", page, cancellationToken);
        }


        [Route("disney")]
        public async Task<IActionResult> Disney(int page = 1, CancellationToken cancellationToken = default)
        {
            return await BuildList("Disney", "~/Views/List/List.cshtml", page, cancellationToken);
        }


        [Route("horrors")]
        public async Task<IActionResult> Horrors(int page = 1, CancellationToken cancellationToken = default)
        {
            return await BuildList("Horrors", "~/Views/List/List.cshtml", page, cancellationToken);
        }
    }
}
