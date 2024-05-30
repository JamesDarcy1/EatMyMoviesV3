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
		private readonly IStorageService _storageService;
		private readonly IListRepository _listRepository;
		private readonly IRankingRepository _rankingRepository;

        public StorageController(IMovieRepository movieRepository,
								 IMovieService movieService,
								 IListRepository listRepository,
								 IRankingRepository rankingRepository, 
								 IStorageService storageService)
        {
			_movieRepository = movieRepository;
			_movieService = movieService;
			_listRepository = listRepository;
			_rankingRepository = rankingRepository;
			_storageService = storageService;
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
			
			if(_rankingRepository.FilmExistsInList(movie.MovieId, list.ListId))
			{
				throw new Exception("Film already exists in list");
			}

			var listRankings = _rankingRepository.GetAllRankingsInList(list);

			if (listRankings.Any(x => x.Ranking == ranking))
			{
				_storageService.ShuffleListDownIfNecessary(list, ranking);
				_rankingRepository.InsertMovieToList(movie, list, ranking);
			}

			var listRanking = _rankingRepository.InsertMovieToList(movie, list, ranking);

			return listRanking;
		}

		[HttpPost]
		public ListRanking UpdateRankingInList(string listName, string movieTitle, int newRanking)
		{
			var movie = _movieRepository.GetMovieByTitle(movieTitle);
			var list = _listRepository.GetListByName(listName);

			if (!_rankingRepository.FilmExistsInList(movie.MovieId, list.ListId))
			{
				throw new Exception("Apparently film doesn't exist in the list");
			}

			var currentRanking = _rankingRepository.GetRankingOfMovie(movie.MovieId, list.Name);
			_storageService.ShuffleListDownIfNecessary(list, currentRanking);
			_rankingRepository.InsertMovieToList(movie, list, newRanking);

			var listRanking = _rankingRepository.InsertMovieToList(movie, list, newRanking);

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
