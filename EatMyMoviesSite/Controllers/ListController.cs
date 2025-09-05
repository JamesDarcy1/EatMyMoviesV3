using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{
    [Route("lists")]
	public class ListController : Controller
    {
        private readonly IMovieService _movieService;

        public ListController(IMovieService movieService)
        {
            _movieService = movieService;
		}

        [HttpGet("{listTitle}")]
        public async Task<IActionResult> Details(string listTitle, int page = 1)
        {
            listTitle = listTitle.Replace("-", " ");
            var movieList = await _movieService.BuildMovieList(listTitle, page);
            return View("~/Views/list/list.cshtml", movieList);
        }
    }
}
