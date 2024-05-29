using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IListRepository
	{
		List AddList(string listName);
		List<List> GetAllLists();
		List GetListByName(string listName);
	}
}