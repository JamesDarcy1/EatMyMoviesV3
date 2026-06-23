using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
        Task<int> CountMoviesAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
        Task<List<Genre>> GetAllGenresAsync(CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetAllMovieSummariesAsync(CancellationToken cancellationToken = default);
        Task<Movie?> GetMovieByIdAsync(Guid movieId, CancellationToken cancellationToken = default);
        Task<Movie?> GetMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default);
        Task<Movie?> GetMovieByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetMoviesOfGenresAsync(IReadOnlyCollection<string> genres, CancellationToken cancellationToken = default);
        Task<List<AdminMovieSummary>> SearchMoviesAsync(string? searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        Task SaveGenresAsync(Guid movieId, IEnumerable<string> genres, CancellationToken cancellationToken = default);
        Task<Movie> SaveTmdbMovieAsync(string title, int tmdbId, decimal? imdbRating, CancellationToken cancellationToken = default);
    }
}
