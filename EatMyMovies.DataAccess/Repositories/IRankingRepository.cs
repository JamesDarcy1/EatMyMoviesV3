using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IRankingRepository
	{
		Task<bool> FilmExistsInListAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
        Task<List<StoredMovieSummary>> GetMovieSummariesInListAsync(string listName, CancellationToken cancellationToken = default);
        Task<List<ListRanking>> GetRankingsAtOrAfterAsync(Guid listId, int ranking, CancellationToken cancellationToken = default);
        Task<int> GetListCountAsync(string listName, CancellationToken cancellationToken = default);
        Task<ListRanking?> GetListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
        Task<List<MovieRankingSummary>> GetListRankingsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
        Task<ListRanking?> GetMovieAtRankingAsync(Guid listId, int ranking, CancellationToken cancellationToken = default);
		Task<List<ListPageMovie>> GetMoviesForListByPageAsync(string listName, int page = 1, int moviesPerPage = 10, CancellationToken cancellationToken = default);
		Task<int> GetRankingOfMovieAsync(Guid movieId, string listName, CancellationToken cancellationToken = default);
		Task<ListRanking> InsertMovieToListAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default);
        Task RemoveListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default);
        Task RemoveRankingAsync(int ranking, Guid listId, CancellationToken cancellationToken = default);
		Task<ListRanking> UpdateRankingAsync(ListRanking listRanking, int newRanking, CancellationToken cancellationToken = default);
	}
}
