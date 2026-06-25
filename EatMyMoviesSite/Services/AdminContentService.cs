using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Models.Admin;

namespace EatMyMoviesSite.Services
{
    internal sealed class AdminContentService : IAdminContentService
    {
        private const int MoviesPageSize = 20;

        private readonly IListRepository _listRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly IMovieOfTheWeekRepository _movieOfTheWeekRepository;
        private readonly IMovieService _movieService;
        private readonly IRankingRepository _rankingRepository;

        public AdminContentService(
            IListRepository listRepository,
            IMovieRepository movieRepository,
            IMovieOfTheWeekRepository movieOfTheWeekRepository,
            IMovieService movieService,
            IRankingRepository rankingRepository)
        {
            _listRepository = listRepository;
            _movieRepository = movieRepository;
            _movieOfTheWeekRepository = movieOfTheWeekRepository;
            _movieService = movieService;
            _rankingRepository = rankingRepository;
        }

        public async Task<AdminDashboardViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default)
        {
            var movieOfTheWeek = await _movieOfTheWeekRepository.GetSelectionAsync(cancellationToken);

            return new AdminDashboardViewModel
            {
                MovieCount = await _movieRepository.CountMoviesAsync(cancellationToken: cancellationToken),
                ListCount = await _listRepository.CountListsAsync(cancellationToken),
                RankingCount = await _rankingRepository.CountRankingsAsync(cancellationToken),
                MovieOfTheWeekTitle = movieOfTheWeek?.Movie.Title,
                MovieOfTheWeekTmdbId = movieOfTheWeek?.Movie.TmdbId
            };
        }

        public async Task<AdminListsViewModel> BuildListsAsync(CancellationToken cancellationToken = default)
        {
            return new AdminListsViewModel
            {
                Lists = await _listRepository.GetListSummariesAsync(cancellationToken)
            };
        }

        public async Task<AdminListDetailViewModel> BuildListDetailAsync(
            Guid listId,
            string? tmdbQuery = null,
            CancellationToken cancellationToken = default)
        {
            var list = await _listRepository.GetListByIdAsync(listId, cancellationToken)
                ?? throw new InvalidOperationException("List was not found.");

            IReadOnlyList<MovieDropdown> tmdbResults = string.IsNullOrWhiteSpace(tmdbQuery)
                ? Array.Empty<MovieDropdown>()
                : await _movieService.SearchMoviesByTitle(tmdbQuery);

            return new AdminListDetailViewModel
            {
                ListId = list.ListId,
                Name = list.Name,
                Description = list.Description,
                Movies = await _rankingRepository.GetListMovieRowsAsync(listId, cancellationToken),
                TmdbQuery = tmdbQuery,
                TmdbSearchResults = tmdbResults
            };
        }

        public async Task<AdminMovieOfTheWeekViewModel> BuildMovieOfTheWeekAsync(
            string? tmdbQuery = null,
            CancellationToken cancellationToken = default)
        {
            var currentSelection = await _movieOfTheWeekRepository.GetSelectionAsync(cancellationToken);
            IReadOnlyList<MovieDropdown> tmdbResults = string.IsNullOrWhiteSpace(tmdbQuery)
                ? Array.Empty<MovieDropdown>()
                : await _movieService.SearchMoviesByTitle(tmdbQuery);

            return new AdminMovieOfTheWeekViewModel
            {
                CurrentTitle = currentSelection?.Movie.Title,
                CurrentTmdbId = currentSelection?.Movie.TmdbId,
                UpdatedUtc = currentSelection?.UpdatedUtc,
                TmdbQuery = tmdbQuery,
                TmdbSearchResults = tmdbResults
            };
        }

        public async Task<AdminMoviesViewModel> BuildMoviesAsync(
            string? searchTerm,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            var totalMovies = await _movieRepository.CountMoviesAsync(searchTerm, cancellationToken);
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalMovies / MoviesPageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            return new AdminMoviesViewModel
            {
                SearchTerm = searchTerm,
                CurrentPage = page,
                TotalPages = totalPages,
                Movies = await _movieRepository.SearchMoviesAsync(searchTerm, page, MoviesPageSize, cancellationToken)
            };
        }

        public async Task<AdminMovieDetailViewModel> BuildMovieDetailAsync(Guid movieId, CancellationToken cancellationToken = default)
        {
            var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken)
                ?? throw new InvalidOperationException("Movie was not found.");

            return new AdminMovieDetailViewModel
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                TmdbId = movie.TmdbId,
                Lists = await _listRepository.GetAllListsAsync(cancellationToken),
                Memberships = await _rankingRepository.GetMovieMembershipsAsync(movieId, cancellationToken)
            };
        }

        public Task CreateListAsync(string listName, string description, CancellationToken cancellationToken = default)
        {
            return _listRepository.AddListAsync(listName, description, cancellationToken);
        }

        public Task UpdateListAsync(Guid listId, string listName, string description, CancellationToken cancellationToken = default)
        {
            return _listRepository.UpdateListAsync(listId, listName, description, cancellationToken);
        }

        public async Task SetMovieOfTheWeekAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            var movie = await EnsureStoredTmdbMovieAsync(tmdbId, cancellationToken);
            await _movieOfTheWeekRepository.SetSelectionAsync(movie.MovieId, cancellationToken);
        }

        public Task ClearMovieOfTheWeekAsync(CancellationToken cancellationToken = default)
        {
            return _movieOfTheWeekRepository.ClearSelectionAsync(cancellationToken);
        }

        public async Task AddTmdbMovieToListAsync(
            Guid listId,
            int tmdbId,
            int ranking,
            CancellationToken cancellationToken = default)
        {
            var movie = await EnsureStoredTmdbMovieAsync(tmdbId, cancellationToken);
            await AddStoredMovieToListAsync(listId, movie.MovieId, ranking, cancellationToken);
        }

        public async Task AddStoredMovieToListAsync(
            Guid listId,
            Guid movieId,
            int ranking,
            CancellationToken cancellationToken = default)
        {
            _ = await _listRepository.GetListByIdAsync(listId, cancellationToken)
                ?? throw new InvalidOperationException("List was not found.");
            _ = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken)
                ?? throw new InvalidOperationException("Movie was not found.");

            if (await _rankingRepository.FilmExistsInListAsync(movieId, listId, cancellationToken))
            {
                throw new InvalidOperationException("Movie is already in this list.");
            }

            await _rankingRepository.AddMovieToListAtRankingAsync(movieId, listId, ranking, cancellationToken);
        }

        public Task MoveMovieWithinListAsync(
            Guid listId,
            Guid movieId,
            int ranking,
            CancellationToken cancellationToken = default)
        {
            return _rankingRepository.MoveMovieWithinListAsync(movieId, listId, ranking, cancellationToken);
        }

        public Task RemoveMovieFromListAsync(Guid listId, Guid movieId, CancellationToken cancellationToken = default)
        {
            return _rankingRepository.RemoveListRankingAndCloseGapAsync(movieId, listId, cancellationToken);
        }

        private async Task<Movie> EnsureStoredTmdbMovieAsync(int tmdbId, CancellationToken cancellationToken)
        {
            var existingMovie = await _movieRepository.GetMovieByTmdbIdAsync(tmdbId, cancellationToken);
            if (existingMovie != null)
            {
                return existingMovie;
            }

            var tmdbMovie = await _movieService.GetMovieById(tmdbId);
            var rating = await _movieService.GetImdbRating(tmdbMovie.Title);
            var movie = await _movieRepository.SaveTmdbMovieAsync(tmdbMovie.Title, tmdbMovie.Id, rating, cancellationToken);
            var genres = tmdbMovie.Genres
                .Select(genre => genre.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!);

            await _movieRepository.SaveGenresAsync(movie.MovieId, genres, cancellationToken);
            return movie;
        }
    }
}
