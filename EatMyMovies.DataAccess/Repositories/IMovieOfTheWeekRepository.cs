using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
    public interface IMovieOfTheWeekRepository
    {
        Task ClearSelectionAsync(CancellationToken cancellationToken = default);
        Task<MovieOfTheWeekSelection?> GetSelectionAsync(CancellationToken cancellationToken = default);
        Task SetSelectionAsync(Guid movieId, CancellationToken cancellationToken = default);
    }
}
