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
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }


        [Route("detail")]
        public async Task<IActionResult> Detail(string title, int? tmdbId = null)
        {
            try
            {
                TMDbLib.Objects.Movies.Movie movie;
                if(tmdbId != null)
                {
                    movie = await _movieService.GetMovieById(tmdbId.Value);
                }
                else { 
                    movie = await _movieService.GetMovieByTitle(title);
                }
                var trailer = await _movieService.GetTrailer(movie.Id);
                var rating = await _movieService.GetImdbRating(movie.Title);
                Person director = await _movieService.GetDirector(movie.Id);
                List<Person> actors = await _movieService.GetActors(movie.Id);
                var movieDetail = Mapper.MapToMovieDetail(movie, trailer, rating, director, actors);
                movieDetail.Lists = _movieService.GetAllLists();
                
                var storeMovie = _movieService.GetStoreMovieByTitle(movie.Title);
                if (storeMovie != null)
                {
                    movieDetail.Rankings = _movieService.GetListRankingsForMovie(storeMovie.MovieId);
                }
                return View(movieDetail);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        [Route("search")]
        public async Task<IActionResult> Search()
        {
            return View();
        }

        [HttpGet("SearchByTitle")]
        public async Task<List<MovieDropdown>> SearchByTitle(string titleSearch, string type)
        {
            var results = await _movieService.SearchByTitle(titleSearch, type);
            return results;
        }


        [Route("recommender")]
        public IActionResult Recommender()
		{
			return View();
		}

        [HttpGet("GetGenres")]
        public List<string> GetGenres()
        {
            var genres = _movieService.GetAllGenres();
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
        public async Task<List<MovieDetail>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange)
        {
            var recommendations = await _movieService.GetFastRecommendations(feelings, duration, openToForeignFilm, yearRange);

            var movieDetailTasks = recommendations.Select(async movie =>
            {
                var trailer = _movieService.GetTrailer(movie.Id);
                var rating = _movieService.GetImdbRating(movie.Title);
                var director = _movieService.GetDirector(movie.Id);
                var actors = _movieService.GetActors(movie.Id);

                await Task.WhenAll(trailer, rating, director, actors);

                return Mapper.MapToMovieDetail(
                    movie,
                    await trailer,
                    await rating,
                    await director,
                    await actors
                );
            });

            var movieDetails = await Task.WhenAll(movieDetailTasks);
            return movieDetails.ToList();
        }

    }
}
