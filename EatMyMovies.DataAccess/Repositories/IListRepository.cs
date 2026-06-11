using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IListRepository
	{
		Task<List> AddListAsync(string listName, string description, CancellationToken cancellationToken = default);
		Task<List<List>> GetAllListsAsync(CancellationToken cancellationToken = default);
		Task<List?> GetListByNameAsync(string listName, CancellationToken cancellationToken = default);
	}
}
