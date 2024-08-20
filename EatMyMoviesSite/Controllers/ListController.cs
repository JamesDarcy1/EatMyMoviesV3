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

		public IActionResult Open(string listName)
		{
			ViewBag.ListName = StringHelpers.AddSpacesToSentence(listName);
			return View("~/Views/List/List.cshtml");
		}

		public async Task<MovieList> GetListData(string listName) {
            var list = await _movieService.BuildMovieList(listName, 1);
			return list;
        }
	}
}
