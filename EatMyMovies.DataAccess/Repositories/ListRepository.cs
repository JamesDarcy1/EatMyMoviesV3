using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;
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
            var normalizedName = NormalizeRequired(listName, nameof(listName));

			return _dbContext.Lists
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == normalizedName, cancellationToken);
		}

        public Task<List?> GetListByIdAsync(Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Lists
                .AsNoTracking()
                .FirstOrDefaultAsync(list => list.ListId == listId, cancellationToken);
        }

		public async Task<List> AddListAsync(string listName, string description, CancellationToken cancellationToken = default)
		{
            var normalizedName = NormalizeRequired(listName, nameof(listName));
            var normalizedDescription = description?.Trim() ?? string.Empty;

			var result = _dbContext.Lists.Add(new List() { Name = normalizedName, Description = normalizedDescription });
			await _dbContext.SaveChangesAsync(cancellationToken);
			return result.Entity;
		}

        public async Task<List> UpdateListAsync(Guid listId, string listName, string description, CancellationToken cancellationToken = default)
        {
            var normalizedName = NormalizeRequired(listName, nameof(listName));
            var normalizedDescription = description?.Trim() ?? string.Empty;
            var list = await _dbContext.Lists
                .FirstOrDefaultAsync(list => list.ListId == listId, cancellationToken)
                ?? throw new InvalidOperationException("List was not found.");

            list.Name = normalizedName;
            list.Description = normalizedDescription;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return list;
        }

		public Task<List<List>> GetAllListsAsync(CancellationToken cancellationToken = default)
		{
			return _dbContext.Lists
                .AsNoTracking()
                .OrderBy(list => list.Name)
                .ToListAsync(cancellationToken);
		}

        public Task<List<AdminListSummary>> GetListSummariesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Lists
                .AsNoTracking()
                .OrderBy(list => list.Name)
                .Select(list => new AdminListSummary(
                    list.ListId,
                    list.Name,
                    list.Description,
                    list.ListRankings.Count))
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountListsAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Lists
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            var normalizedValue = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                throw new ArgumentException("Value cannot be blank.", parameterName);
            }

            return normalizedValue;
        }
	}
}
