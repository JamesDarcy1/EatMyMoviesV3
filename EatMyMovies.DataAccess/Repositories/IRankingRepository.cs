using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IRankingRepository
	{
        Task<ListRanking> AddMovieToListAtRankingAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default);
		Task<bool> FilmExistsInListAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetMovieSummariesInListAsync(string listName, CancellationToken cancellationToken = default);
        Task<int> GetListCountAsync(string listName, CancellationToken cancellationToken = default);
        Task<ListRanking?> GetListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
        Task<List<MovieRankingSummary>> GetListRankingsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
		Task<List<ListPageMovie>> GetMoviesForListByPageAsync(string listName, int page = 1, int moviesPerPage = 10, CancellationToken cancellationToken = default);
		Task<int> GetRankingOfMovieAsync(Guid movieId, string listName, CancellationToken cancellationToken = default);
        Task<ListRanking> MoveMovieWithinListAsync(Guid movieId, Guid listId, int newRanking, CancellationToken cancellationToken = default);
        Task RemoveListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
	}
}
