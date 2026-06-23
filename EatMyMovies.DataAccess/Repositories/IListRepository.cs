using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IListRepository
	{
		Task<List> AddListAsync(string listName, string description, CancellationToken cancellationToken = default);
		Task<int> CountListsAsync(CancellationToken cancellationToken = default);
		Task<List<List>> GetAllListsAsync(CancellationToken cancellationToken = default);
		Task<List?> GetListByIdAsync(Guid listId, CancellationToken cancellationToken = default);
		Task<List?> GetListByNameAsync(string listName, CancellationToken cancellationToken = default);
		Task<List<AdminListSummary>> GetListSummariesAsync(CancellationToken cancellationToken = default);
		Task<List> UpdateListAsync(Guid listId, string listName, string description, CancellationToken cancellationToken = default);
	}
}
