using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EatMyMoviesSite.Controllers
{
    [Route("[controller]")]
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
            if (_movieRepository.GetMovieByTitle(title) != null)
            {
                throw new Exception(title + " already in db");
            }
            var tmdbMovie = await _movieService.GetMovieByTitle(title);
            var rating = await _movieService.GetImdbRating(tmdbMovie.Title);
            var genres = tmdbMovie.Genres.Select(x => x.Name);
            var entity = _movieRepository.SaveTmdbMovie(tmdbMovie.Title, tmdbMovie.Id, rating);
            _movieRepository.SaveGenres(entity, genres);
            return entity;
        }

        [HttpGet]
        public async Task<TMDbLib.Objects.Movies.Movie> Search(string title)
        {
            var tmdbMovie = await _movieService.GetMovieByTitle(title);
            return tmdbMovie;
        }

        [HttpPost("AddMovieToList")]
        public async Task<IActionResult> AddMovieToList(string listName, string movieTitle, int ranking)
        {
            var movie = _movieRepository.GetMovieByTitle(movieTitle);
            var list = _listRepository.GetListByName(listName);

            if (movie == null)
            {
                movie = await SaveToDatabase(movieTitle);
            }

            if (_rankingRepository.FilmExistsInList(movie.MovieId, list.ListId))
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var listRankings = _rankingRepository.GetAllRankingsInList(list);

            if (listRankings.Any(x => x.Ranking == ranking))
            {
                _storageService.ShuffleListDownIfNecessary(list, ranking);
            }

            _rankingRepository.InsertMovieToList(movie, list, ranking);

            return Redirect(Request.Headers["Referer"].ToString());
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
            _storageService.ShuffleListDownIfNecessary(list, newRanking);
            var updatedListRanking = _rankingRepository.InsertMovieToList(movie, list, newRanking);

            _rankingRepository.RemoveRanking(currentRanking, list.ListId);

            return updatedListRanking;

        }

        [HttpPost]
        public List AddList(string listName, string description)
        {
            var list = _listRepository.AddList(listName, description);
            return list;
        }

        [HttpPost]
        public async Task LoadGenres()
        {
            var movies = _movieRepository.GetAllMovies().ToList();
            foreach (var movie in movies)
            {
                var tmdbMovie = await _movieService.GetMovieByTitle(movie.Title);
                if (tmdbMovie != null)
                {
                    Console.WriteLine("movie ", tmdbMovie.Title);
                    var genres = tmdbMovie.Genres.Select(x => x.Name);
                    _movieRepository.SaveGenres(movie, genres);
                    Console.WriteLine("saved genres ", JsonSerializer.Serialize(tmdbMovie.Genres));
                }
            }
        }

        [HttpPost("UpdateRanking")]
        public IActionResult UpdateRanking(Guid movieId, Guid listId, int newRanking)
        {
            var ranking = _rankingRepository.GetListRanking(movieId, listId);

            if (ranking == null)
            {
                return NotFound("Ranking not found");
            }

            _rankingRepository.UpdateRanking(ranking, newRanking);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost("DeleteRanking")]
        public IActionResult DeleteRanking(Guid movieId, Guid listId)
        {
            _rankingRepository.RemoveListRanking(movieId, listId);

            return Redirect(Request.Headers["Referer"].ToString());
        }

    }
}
