using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess.Repositories
{
	public class ListRepository : IListRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public ListRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Task<List?> GetListByNameAsync(string listName, CancellationToken cancellationToken = default)
		{
			return _dbContext.Lists
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == listName, cancellationToken);
		}

		public async Task<List> AddListAsync(string listName, string description, CancellationToken cancellationToken = default)
		{
			var result = _dbContext.Lists.Add(new List() { Name = listName, Description = description });
			await _dbContext.SaveChangesAsync(cancellationToken);
			return result.Entity;
		}

		public Task<List<List>> GetAllListsAsync(CancellationToken cancellationToken = default)
		{
			return _dbContext.Lists
                .AsNoTracking()
                .OrderBy(list => list.Name)
                .ToListAsync(cancellationToken);
		}
	}
}
