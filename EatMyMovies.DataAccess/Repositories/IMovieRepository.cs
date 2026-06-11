using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
        Task<List<Genre>> GetAllGenresAsync(CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetAllMovieSummariesAsync(CancellationToken cancellationToken = default);
        Task<Movie?> GetMovieByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetMoviesOfGenresAsync(IReadOnlyCollection<string> genres, CancellationToken cancellationToken = default);
        Task SaveGenresAsync(Guid movieId, IEnumerable<string> genres, CancellationToken cancellationToken = default);
        Task<Movie> SaveTmdbMovieAsync(string title, int tmdbId, decimal? imdbRating, CancellationToken cancellationToken = default);
    }
}
