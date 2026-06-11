using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess.Repositories
{
    public class RankingRepository : IRankingRepository
    {
        private readonly EatMyMoviesContext _dbContext;

        public RankingRepository(EatMyMoviesContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<List<ListPageMovie>> GetMoviesForListByPageAsync(
            string listName,
            int page = 1,
            int moviesPerPage = 10,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == listName)
                .OrderBy(listRanking => listRanking.Ranking)
                .Skip((page - 1) * moviesPerPage)
                .Take(moviesPerPage)
                .Select(listRanking => new ListPageMovie(
                    listRanking.MovieId,
                    listRanking.Movie.Title,
                    listRanking.Movie.TmdbId,
                    listRanking.Ranking))
                .ToListAsync(cancellationToken);
        }

        public Task<List<StoredMovieSummary>> GetMovieSummariesInListAsync(string listName, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == listName)
                .Select(listRanking => new StoredMovieSummary(
                    listRanking.MovieId,
                    listRanking.Movie.Title,
                    listRanking.Movie.TmdbId))
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetListCountAsync(string listName, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .CountAsync(listRanking => listRanking.List.Name == listName, cancellationToken);
        }

        public async Task<int> GetRankingOfMovieAsync(Guid movieId, string listName, CancellationToken cancellationToken = default)
        {
            var ranking = await _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == listName && listRanking.MovieId == movieId)
                .Select(listRanking => (int?)listRanking.Ranking)
                .FirstOrDefaultAsync(cancellationToken);

            if (ranking == null)
            {
                throw new Exception("Film does not exist in list");
            }

            return ranking.Value;
        }

        public async Task<ListRanking> InsertMovieToListAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default)
        {
            var result = _dbContext.ListRankings.Add(new ListRanking()
            {
                ListId = listId,
                MovieId = movieId,
                Ranking = ranking
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return result.Entity;
        }

        public Task<ListRanking?> GetMovieAtRankingAsync(Guid listId, int ranking, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .FirstOrDefaultAsync(listRanking => listRanking.ListId == listId && listRanking.Ranking == ranking, cancellationToken);
        }

        public Task<bool> FilmExistsInListAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .AnyAsync(listRanking => listRanking.MovieId == movieId && listRanking.ListId == listId, cancellationToken);
        }

        public Task<List<ListRanking>> GetRankingsAtOrAfterAsync(Guid listId, int ranking, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .Where(listRanking => listRanking.ListId == listId && listRanking.Ranking >= ranking)
                .OrderBy(listRanking => listRanking.Ranking)
                .ToListAsync(cancellationToken);
        }

        public async Task<ListRanking> UpdateRankingAsync(ListRanking listRanking, int newRanking, CancellationToken cancellationToken = default)
        {
            listRanking.Ranking = newRanking;
            var updatedListRanking = _dbContext.ListRankings.Update(listRanking);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return updatedListRanking.Entity;
        }

        public async Task RemoveRankingAsync(int ranking, Guid listId, CancellationToken cancellationToken = default)
        {
            var listRanking = await _dbContext.ListRankings
                .FirstOrDefaultAsync(lr => lr.ListId == listId && lr.Ranking == ranking, cancellationToken);
            if (listRanking != null)
            {
                _dbContext.ListRankings.Remove(listRanking);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public Task<List<MovieRankingSummary>> GetListRankingsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.MovieId == movieId)
                .Select(listRanking => new MovieRankingSummary(
                    listRanking.MovieId,
                    listRanking.ListId,
                    listRanking.List.Name,
                    listRanking.Ranking,
                    listRanking.ListRankingId))
                .ToListAsync(cancellationToken);
        }

        public Task<ListRanking?> GetListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .FirstOrDefaultAsync(listRanking => listRanking.MovieId == movieId && listRanking.ListId == listId, cancellationToken);
        }

        public async Task RemoveListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            var listRanking = await GetListRankingAsync(movieId, listId, cancellationToken);
            
            if (listRanking != null)
            {
                _dbContext.ListRankings.Remove(listRanking);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
