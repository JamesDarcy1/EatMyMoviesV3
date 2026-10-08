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

        [HttpGet("")]
        public IActionResult Index()
        {
            return NotFound();
        }


        [Route("detail")]
        public async Task<IActionResult> Detail(string? title, int? tmdbId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasId = Request.Query.ContainsKey("tmdbId");
            if (!ModelState.IsValid || (hasId && (!tmdbId.HasValue || tmdbId <= 0)) ||
                (!hasId && string.IsNullOrWhiteSpace(title)))
            {
                return NotFound();
            }
            if (hasId && Request.Query.ContainsKey("title"))
            {
                return RedirectPermanent(SeoMetadataService.MoviePath(tmdbId!.Value));
            }
            if (!hasId)
            {
                var movie = await _movieService.GetMovieByTitle(title!);
                cancellationToken.ThrowIfCancellationRequested();
                return Redirect(SeoMetadataService.MoviePath(movie.Id));
            }
            var movieDetail = await _movieService.BuildMovieDetail(null, tmdbId, includeListContext: false, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return View(movieDetail);
        }


        [Route("search")]
        public async Task<IActionResult> Search()
        {
            return View();
        }

        [HttpGet("SearchForMovie")]
        public async Task<List<MovieDropdown>> SearchForMovie(string titleSearch)
        {
            Response.Headers["X-Robots-Tag"] = "noindex";
            var results = await _movieService.SearchMoviesByTitle(titleSearch);
            return results;
        }


        [Route("recommender")]
        public IActionResult Recommender()
		{
			return View();
		}

        [Route("spin-the-wheel")]
        public IActionResult SpinTheWheel()
        {
            return View("~/Views/Movie/SpinTheWheel.cshtml");
        }

        [HttpGet("GetGenres")]
        public async Task<List<string>> GetGenres(CancellationToken cancellationToken = default)
        {
            Response.Headers["X-Robots-Tag"] = "noindex";
            var genres = await _movieService.GetAllGenresAsync(cancellationToken);
            var shuffledGenres = _movieService.ShuffleList<Genre>(genres);
            return genres.Select(x => x.Name).ToList();
        }

        [HttpGet("GetFeelings")]
        public List<string> GetFeelings()
        {
            Response.Headers["X-Robots-Tag"] = "noindex";
            var feelings = Enum.GetNames(typeof(Feeling)).ToList();
            return feelings;
        }

        [HttpGet("GetRecommendations")]
        public async Task<List<MovieDetail>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange, CancellationToken cancellationToken = default)
        {
            Response.Headers["X-Robots-Tag"] = "noindex";
            var recommendations = await _movieService.GetFastRecommendations(feelings, duration, openToForeignFilm, yearRange, cancellationToken);
            var movieDetails = await Task.WhenAll(recommendations.Select(movie =>
                _movieService.BuildMovieDetail(movie.Title, movie.Id, includeListContext: false, cancellationToken)));

            return movieDetails.ToList();
        }

    }
}
