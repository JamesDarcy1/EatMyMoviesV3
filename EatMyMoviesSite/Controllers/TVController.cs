using EatMyMovies.DataAccess.Models;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using EatMyMoviesSite.Models;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{

    [Route("tv")]
    public class TVController : Controller
    {
        private readonly IMovieService _movieService;
        public TVController(IMovieService movieService)
        {
            _movieService = movieService;
        }


        [Route("detail")]
        public async Task<IActionResult> Detail(string title, int? tmdbId = null)
        {
            try
            {
                TMDbLib.Objects.TvShows.TvShow show = await _movieService.GetTVSeriesById(tmdbId.Value);
                var trailer = await _movieService.GetTrailer(show.Id);
                var rating = await _movieService.GetImdbRating(show.Name);
                Person director = await _movieService.GetDirector(show.Id);
                List<Person> actors = await _movieService.GetActors(show.Id);
                var tvDetail = Mapper.MapToTvShowDetail(show, trailer, rating, director, actors);
                tvDetail.Lists = _movieService.GetAllLists();
                
                var storeMovie = _movieService.GetStoreMovieByTitle(show.Name);
                if (storeMovie != null)
                {
                    tvDetail.Rankings = _movieService.GetListRankingsForMovie(storeMovie.MovieId);
                }
                return View("~/Views/movie/detail.cshtml", tvDetail);
            }
            catch (Exception ex)
            {
                return View();
            }
        }
    }
}
