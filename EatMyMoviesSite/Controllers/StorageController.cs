using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<Movie> SaveToDatabase(string title, CancellationToken cancellationToken = default)
        {
            if (await _movieRepository.GetMovieByTitleAsync(title, cancellationToken) != null)
            {
                throw new Exception(title + " already in db");
            }
            var tmdbMovie = await _movieService.GetMovieByTitle(title);
            var rating = await _movieService.GetImdbRating(tmdbMovie.Title);
            var genres = tmdbMovie.Genres.Select(x => x.Name).Where(name => name != null).Select(name => name!);
            var entity = await _movieRepository.SaveTmdbMovieAsync(tmdbMovie.Title, tmdbMovie.Id, rating, cancellationToken);
            await _movieRepository.SaveGenresAsync(entity.MovieId, genres, cancellationToken);
            return entity;
        }

        [HttpGet]
        public async Task<TMDbLib.Objects.Movies.Movie> Search(string title)
        {
            var tmdbMovie = await _movieService.GetMovieByTitle(title);
            return tmdbMovie;
        }

        [HttpPost("AddMovieToList")]
        public async Task<IActionResult> AddMovieToList(string listName, string movieTitle, int ranking, CancellationToken cancellationToken = default)
        {
            var movie = await _movieRepository.GetMovieByTitleAsync(movieTitle, cancellationToken);
            var list = await _listRepository.GetListByNameAsync(listName, cancellationToken)
                ?? throw new Exception($"List '{listName}' was not found.");

            if (movie == null)
            {
                movie = await SaveToDatabase(movieTitle, cancellationToken);
            }

            if (await _rankingRepository.FilmExistsInListAsync(movie.MovieId, list.ListId, cancellationToken))
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            await _storageService.ShuffleListDownIfNecessaryAsync(list.ListId, ranking, cancellationToken);
            await _rankingRepository.InsertMovieToListAsync(movie.MovieId, list.ListId, ranking, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }


        [HttpPost]
        public async Task<ListRanking> UpdateRankingInList(string listName, string movieTitle, int newRanking, CancellationToken cancellationToken = default)
        {
            var movie = await _movieRepository.GetMovieByTitleAsync(movieTitle, cancellationToken)
                ?? throw new Exception($"Movie '{movieTitle}' was not found.");
            var list = await _listRepository.GetListByNameAsync(listName, cancellationToken)
                ?? throw new Exception($"List '{listName}' was not found.");

            if (!await _rankingRepository.FilmExistsInListAsync(movie.MovieId, list.ListId, cancellationToken))
            {
                throw new Exception("Apparently film doesn't exist in the list");
            }

            var currentRanking = await _rankingRepository.GetRankingOfMovieAsync(movie.MovieId, list.Name, cancellationToken);
            await _storageService.ShuffleListDownIfNecessaryAsync(list.ListId, newRanking, cancellationToken);
            var updatedListRanking = await _rankingRepository.InsertMovieToListAsync(movie.MovieId, list.ListId, newRanking, cancellationToken);

            await _rankingRepository.RemoveRankingAsync(currentRanking, list.ListId, cancellationToken);

            return updatedListRanking;
        }

        [HttpPost]
        public Task<List> AddList(string listName, string description, CancellationToken cancellationToken = default)
        {
            return _listRepository.AddListAsync(listName, description, cancellationToken);
        }

        [HttpPost]
        public async Task LoadGenres(CancellationToken cancellationToken = default)
        {
            var movies = await _movieRepository.GetAllMovieSummariesAsync(cancellationToken);
            foreach (var movie in movies)
            {
                var tmdbMovie = await _movieService.GetMovieByTitle(movie.Title);
                if (tmdbMovie != null)
                {
                    Console.WriteLine("movie ", tmdbMovie.Title);
                    var genres = tmdbMovie.Genres.Select(x => x.Name).Where(name => name != null).Select(name => name!);
                    await _movieRepository.SaveGenresAsync(movie.MovieId, genres, cancellationToken);
                    Console.WriteLine("saved genres ", JsonSerializer.Serialize(tmdbMovie.Genres));
                }
            }
        }

        [HttpPost("UpdateRanking")]
        public async Task<IActionResult> UpdateRanking(Guid movieId, Guid listId, int newRanking, CancellationToken cancellationToken = default)
        {
            var ranking = await _rankingRepository.GetListRankingAsync(movieId, listId, cancellationToken);

            if (ranking == null)
            {
                return NotFound("Ranking not found");
            }

            await _rankingRepository.UpdateRankingAsync(ranking, newRanking, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost("DeleteRanking")]
        public async Task<IActionResult> DeleteRanking(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            await _rankingRepository.RemoveListRankingAsync(movieId, listId, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
