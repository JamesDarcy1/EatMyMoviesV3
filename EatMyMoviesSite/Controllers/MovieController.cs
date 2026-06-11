using EatMyMovies.DataAccess.Models;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using EatMyMoviesSite.Models;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{

    [Route("movie")]
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<MovieController> _logger;

        public MovieController(IMovieService movieService, ILogger<MovieController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        [Route("detail")]
        public async Task<IActionResult> Detail(string title, int? tmdbId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var movieDetail = await _movieService.BuildMovieDetail(title, tmdbId, includeListContext: true, cancellationToken);
                return View(movieDetail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load movie detail for title '{Title}' and TMDb id '{TmdbId}'.", title, tmdbId);
                return View();
            }
        }


        [Route("search")]
        public async Task<IActionResult> Search()
        {
            return View();
        }

        [HttpGet("SearchForMovie")]
        public async Task<List<MovieDropdown>> SearchForMovie(string titleSearch)
        {
            var results = await _movieService.SearchMoviesByTitle(titleSearch);
            return results;
        }


        [Route("recommender")]
        public IActionResult Recommender()
		{
			return View();
		}

        [HttpGet("GetGenres")]
        public async Task<List<string>> GetGenres(CancellationToken cancellationToken = default)
        {
            var genres = await _movieService.GetAllGenresAsync(cancellationToken);
            var shuffledGenres = _movieService.ShuffleList<Genre>(genres);
            return genres.Select(x => x.Name).ToList();
        }

        [HttpGet("GetFeelings")]
        public List<string> GetFeelings()
        {
            var feelings = Enum.GetNames(typeof(Feeling)).ToList();
            return feelings;
        }

        [HttpGet("GetRecommendations")]
        public async Task<List<MovieDetail>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange, CancellationToken cancellationToken = default)
        {
            var recommendations = await _movieService.GetFastRecommendations(feelings, duration, openToForeignFilm, yearRange, cancellationToken);
            var movieDetails = await Task.WhenAll(recommendations.Select(movie =>
                _movieService.BuildMovieDetail(movie.Title, movie.Id, includeListContext: false, cancellationToken)));

            return movieDetails.ToList();
        }

    }
}
