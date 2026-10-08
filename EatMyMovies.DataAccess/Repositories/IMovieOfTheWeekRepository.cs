using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
    public interface IMovieOfTheWeekRepository
    {
        Task ClearSelectionAsync(CancellationToken cancellationToken = default);
        Task<MovieOfTheWeekSelection?> GetSelectionAsync(CancellationToken cancellationToken = default);
        Task SetSelectionAsync(Guid movieId, string? editorialNote, CancellationToken cancellationToken = default);
        Task UpdateEditorialNoteAsync(string? editorialNote, CancellationToken cancellationToken = default);
    }
}
