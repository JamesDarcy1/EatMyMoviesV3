using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers
{
	public class StorageController : ControllerBase
	{
		private readonly IMovieRepository _movieRepository;
		private readonly IMovieService _movieService;
		private readonly IListRepository _listRepository;
		private readonly IRankingRepository _rankingRepository;

        public StorageController(IMovieRepository movieRepository,
								 IMovieService movieService,
								 IListRepository listRepository,
								 IRankingRepository rankingRepository)
        {
			_movieRepository = movieRepository;
			_movieService = movieService;
			_listRepository = listRepository;
			_rankingRepository = rankingRepository;
        }

		[HttpPost]
		public async Task<Movie> SaveToDatabase(string title)
		{
			var tmdbMovie = await _movieService.GetMovieByTitle(title);
			var rating = await _movieService.GetImdbRating(title);
			var entity = _movieRepository.SaveTmdbMovie(tmdbMovie.Title, tmdbMovie.Id, rating);
			return entity;
		}

		[HttpGet]
		public async Task<TMDbLib.Objects.Movies.Movie> Search(string title)
		{
			var tmdbMovie = await _movieService.GetMovieByTitle(title);
			return tmdbMovie;
		}

		[HttpPost]
		public ListRanking AddMovieToList(string listName, string movieTitle, int ranking)
		{
			var movie = _movieRepository.GetMovieByTitle(movieTitle);
			var list = _listRepository.GetListByName(listName);
			var listRanking = _rankingRepository.InsertMovieToList(movie, list, ranking);

			return listRanking;
		}

		[HttpPost]
		public List AddList(string listName)
		{
			var list = _listRepository.AddList(listName);
			return list;
		}
	}
}
