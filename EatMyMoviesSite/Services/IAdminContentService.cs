using EatMyMoviesSite.Models.Admin;

namespace EatMyMoviesSite.Services
{
    public interface IAdminContentService
    {
        Task AddStoredMovieToListAsync(Guid listId, Guid movieId, int ranking, CancellationToken cancellationToken = default);
        Task AddTmdbMovieToListAsync(Guid listId, int tmdbId, int ranking, CancellationToken cancellationToken = default);
        Task<AdminDashboardViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default);
        Task<AdminListDetailViewModel> BuildListDetailAsync(Guid listId, string? tmdbQuery = null, CancellationToken cancellationToken = default);
        Task<AdminListsViewModel> BuildListsAsync(CancellationToken cancellationToken = default);
        Task<AdminMovieOfTheWeekViewModel> BuildMovieOfTheWeekAsync(string? tmdbQuery = null, CancellationToken cancellationToken = default);
        Task<AdminMovieDetailViewModel> BuildMovieDetailAsync(Guid movieId, CancellationToken cancellationToken = default);
        Task<AdminMoviesViewModel> BuildMoviesAsync(string? searchTerm, int page = 1, CancellationToken cancellationToken = default);
        Task ClearMovieOfTheWeekAsync(CancellationToken cancellationToken = default);
        Task CreateListAsync(string listName, string description, CancellationToken cancellationToken = default);
        Task MoveMovieWithinListAsync(Guid listId, Guid movieId, int ranking, CancellationToken cancellationToken = default);
        Task RemoveMovieFromListAsync(Guid listId, Guid movieId, CancellationToken cancellationToken = default);
        Task SetMovieOfTheWeekAsync(int tmdbId, CancellationToken cancellationToken = default);
        Task UpdateListAsync(Guid listId, string listName, string description, CancellationToken cancellationToken = default);
    }
}
