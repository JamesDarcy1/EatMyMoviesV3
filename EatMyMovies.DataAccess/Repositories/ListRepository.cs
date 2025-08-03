using EatMyMovies.DataAccess.Models;
using System.Net;

namespace EatMyMovies.DataAccess.Repositories
{
	public class ListRepository : IListRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public ListRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public List GetListByName(string listName)
		{
			var list = _dbContext.Lists.FirstOrDefault(x => x.Name == listName);
			return list;
		}

		public List AddList(string listName, string description)
		{
			var result = _dbContext.Lists.Add(new List() { Name = listName, Description = description });
			_dbContext.SaveChanges();
			return result.Entity;
		}

		public List<List> GetAllLists()
		{
			var lists = _dbContext.Lists.ToList();
			return lists;
		}
	}
}
