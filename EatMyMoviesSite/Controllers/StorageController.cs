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
            var normalizedTitle = NormalizeRequired(title, nameof(title));

            if (await _movieRepository.GetMovieByTitleAsync(normalizedTitle, cancellationToken) != null)
            {
                throw new Exception(normalizedTitle + " already in db");
            }
            var tmdbMovie = await _movieService.GetMovieByTitle(normalizedTitle);
            var rating = await _movieService.GetImdbRating(tmdbMovie.Title);
            var genres = tmdbMovie.Genres.Select(x => x.Name).Where(name => name != null).Select(name => name!);
            var entity = await _movieRepository.SaveTmdbMovieAsync(tmdbMovie.Title, tmdbMovie.Id, rating, cancellationToken);
            await _movieRepository.SaveGenresAsync(entity.MovieId, genres, cancellationToken);
            return entity;
        }

        [HttpGet]
        public async Task<TMDbLib.Objects.Movies.Movie> Search(string title)
        {
            var tmdbMovie = await _movieService.GetMovieByTitle(NormalizeRequired(title, nameof(title)));
            return tmdbMovie;
        }

        [HttpPost("AddMovieToList")]
        public async Task<IActionResult> AddMovieToList(string listName, string movieTitle, int ranking, CancellationToken cancellationToken = default)
        {
            var normalizedListName = NormalizeRequired(listName, nameof(listName));
            var normalizedMovieTitle = NormalizeRequired(movieTitle, nameof(movieTitle));
            var movie = await _movieRepository.GetMovieByTitleAsync(normalizedMovieTitle, cancellationToken);
            var list = await _listRepository.GetListByNameAsync(normalizedListName, cancellationToken)
                ?? throw new Exception($"List '{normalizedListName}' was not found.");

            if (movie == null)
            {
                movie = await SaveToDatabase(normalizedMovieTitle, cancellationToken);
            }

            if (await _rankingRepository.FilmExistsInListAsync(movie.MovieId, list.ListId, cancellationToken))
            {
                return Redirect(Request.Headers["Referer"].ToString());
            }

            await _storageService.AddMovieToListAtRankingAsync(movie.MovieId, list.ListId, ranking, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }


        [HttpPost]
        public async Task<ListRanking> UpdateRankingInList(string listName, string movieTitle, int newRanking, CancellationToken cancellationToken = default)
        {
            var normalizedListName = NormalizeRequired(listName, nameof(listName));
            var normalizedMovieTitle = NormalizeRequired(movieTitle, nameof(movieTitle));
            var movie = await _movieRepository.GetMovieByTitleAsync(normalizedMovieTitle, cancellationToken)
                ?? throw new Exception($"Movie '{normalizedMovieTitle}' was not found.");
            var list = await _listRepository.GetListByNameAsync(normalizedListName, cancellationToken)
                ?? throw new Exception($"List '{normalizedListName}' was not found.");

            if (!await _rankingRepository.FilmExistsInListAsync(movie.MovieId, list.ListId, cancellationToken))
            {
                throw new Exception("Apparently film doesn't exist in the list");
            }

            await _storageService.MoveMovieWithinListAsync(movie.MovieId, list.ListId, newRanking, cancellationToken);
            return await _rankingRepository.GetListRankingAsync(movie.MovieId, list.ListId, cancellationToken)
                ?? throw new InvalidOperationException("Ranking was not found after move.");
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

            await _storageService.MoveMovieWithinListAsync(movieId, listId, newRanking, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost("DeleteRanking")]
        public async Task<IActionResult> DeleteRanking(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            await _rankingRepository.RemoveListRankingAsync(movieId, listId, cancellationToken);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            var normalizedValue = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                throw new ArgumentException("Value cannot be blank.", parameterName);
            }

            return normalizedValue;
        }
    }
}
