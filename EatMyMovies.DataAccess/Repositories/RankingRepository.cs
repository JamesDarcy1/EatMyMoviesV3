using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
            var normalizedListName = NormalizeRequired(listName, nameof(listName));

            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == normalizedListName)
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
            var normalizedListName = NormalizeRequired(listName, nameof(listName));

            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == normalizedListName)
                .Select(listRanking => new StoredMovieSummary(
                    listRanking.MovieId,
                    listRanking.Movie.Title,
                    listRanking.Movie.TmdbId))
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetListCountAsync(string listName, CancellationToken cancellationToken = default)
        {
            var normalizedListName = NormalizeRequired(listName, nameof(listName));

            return _dbContext.ListRankings
                .AsNoTracking()
                .CountAsync(listRanking => listRanking.List.Name == normalizedListName, cancellationToken);
        }

        public Task<int> CountRankingsAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }

        public Task<List<AdminListMovieRow>> GetListMovieRowsAsync(Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.ListId == listId)
                .OrderBy(listRanking => listRanking.Ranking)
                .Select(listRanking => new AdminListMovieRow(
                    listRanking.MovieId,
                    listRanking.Movie.Title,
                    listRanking.Movie.TmdbId,
                    listRanking.Ranking))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetRankingOfMovieAsync(Guid movieId, string listName, CancellationToken cancellationToken = default)
        {
            var normalizedListName = NormalizeRequired(listName, nameof(listName));

            var ranking = await _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.List.Name == normalizedListName && listRanking.MovieId == movieId)
                .Select(listRanking => (int?)listRanking.Ranking)
                .FirstOrDefaultAsync(cancellationToken);

            if (ranking == null)
            {
                throw new Exception("Film does not exist in list");
            }

            return ranking.Value;
        }

        public async Task<ListRanking> AddMovieToListAtRankingAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default)
        {
            ValidateRanking(ranking);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            if (await _dbContext.ListRankings.AnyAsync(
                listRanking => listRanking.MovieId == movieId && listRanking.ListId == listId,
                cancellationToken))
            {
                throw new InvalidOperationException("Movie already exists in list.");
            }

            await OpenRankingSlotAsync(listId, ranking, cancellationToken);

            var result = _dbContext.ListRankings.Add(new ListRanking
            {
                ListId = listId,
                MovieId = movieId,
                Ranking = ranking
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result.Entity;
        }

        public async Task<ListRanking> MoveMovieWithinListAsync(Guid movieId, Guid listId, int newRanking, CancellationToken cancellationToken = default)
        {
            ValidateRanking(newRanking);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var listRanking = await _dbContext.ListRankings
                .FirstOrDefaultAsync(
                    ranking => ranking.MovieId == movieId && ranking.ListId == listId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Movie does not exist in list.");

            var currentRanking = listRanking.Ranking;
            if (currentRanking == newRanking)
            {
                await transaction.CommitAsync(cancellationToken);
                return listRanking;
            }

            var temporaryOffset = await GetTemporaryRankingOffsetAsync(listId, cancellationToken);
            listRanking.Ranking += temporaryOffset;
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (newRanking < currentRanking)
            {
                var affectedRankings = await _dbContext.ListRankings
                    .Where(ranking => ranking.ListId == listId && ranking.Ranking >= newRanking && ranking.Ranking < currentRanking)
                    .ToListAsync(cancellationToken);

                await MoveRankingsTemporarilyAsync(affectedRankings, temporaryOffset, cancellationToken);
                foreach (var affectedRanking in affectedRankings)
                {
                    affectedRanking.Ranking = affectedRanking.Ranking - temporaryOffset + 1;
                }
            }
            else
            {
                var affectedRankings = await _dbContext.ListRankings
                    .Where(ranking => ranking.ListId == listId && ranking.Ranking > currentRanking && ranking.Ranking <= newRanking)
                    .ToListAsync(cancellationToken);

                await MoveRankingsTemporarilyAsync(affectedRankings, temporaryOffset, cancellationToken);
                foreach (var affectedRanking in affectedRankings)
                {
                    affectedRanking.Ranking = affectedRanking.Ranking - temporaryOffset - 1;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            listRanking.Ranking = newRanking;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return listRanking;
        }

        public Task<bool> FilmExistsInListAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .AnyAsync(listRanking => listRanking.MovieId == movieId && listRanking.ListId == listId, cancellationToken);
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

        public Task<List<AdminMovieMembership>> GetMovieMembershipsAsync(Guid movieId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .AsNoTracking()
                .Where(listRanking => listRanking.MovieId == movieId)
                .OrderBy(listRanking => listRanking.List.Name)
                .Select(listRanking => new AdminMovieMembership(
                    listRanking.MovieId,
                    listRanking.ListId,
                    listRanking.List.Name,
                    listRanking.Ranking))
                .ToListAsync(cancellationToken);
        }

        public Task<ListRanking?> GetListRankingAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            return _dbContext.ListRankings
                .FirstOrDefaultAsync(listRanking => listRanking.MovieId == movieId && listRanking.ListId == listId, cancellationToken);
        }

        public async Task RemoveListRankingAndCloseGapAsync(Guid movieId, Guid listId, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var listRanking = await _dbContext.ListRankings
                .FirstOrDefaultAsync(ranking => ranking.MovieId == movieId && ranking.ListId == listId, cancellationToken);

            if (listRanking == null)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var removedRanking = listRanking.Ranking;
            _dbContext.ListRankings.Remove(listRanking);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var rankingsToClose = await _dbContext.ListRankings
                .Where(ranking => ranking.ListId == listId && ranking.Ranking > removedRanking)
                .ToListAsync(cancellationToken);

            foreach (var ranking in rankingsToClose)
            {
                ranking.Ranking -= 1;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
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

        private async Task OpenRankingSlotAsync(Guid listId, int ranking, CancellationToken cancellationToken)
        {
            var affectedRankings = await _dbContext.ListRankings
                .Where(listRanking => listRanking.ListId == listId && listRanking.Ranking >= ranking)
                .ToListAsync(cancellationToken);

            if (affectedRankings.Count == 0)
            {
                return;
            }

            var temporaryOffset = await GetTemporaryRankingOffsetAsync(listId, cancellationToken);
            await MoveRankingsTemporarilyAsync(affectedRankings, temporaryOffset, cancellationToken);

            foreach (var affectedRanking in affectedRankings)
            {
                affectedRanking.Ranking = affectedRanking.Ranking - temporaryOffset + 1;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task MoveRankingsTemporarilyAsync(
            IEnumerable<ListRanking> rankings,
            int temporaryOffset,
            CancellationToken cancellationToken)
        {
            foreach (var ranking in rankings)
            {
                ranking.Ranking += temporaryOffset;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<int> GetTemporaryRankingOffsetAsync(Guid listId, CancellationToken cancellationToken)
        {
            var maxRanking = await _dbContext.ListRankings
                .Where(listRanking => listRanking.ListId == listId)
                .MaxAsync(listRanking => (int?)listRanking.Ranking, cancellationToken) ?? 0;

            if (maxRanking > int.MaxValue / 2 - 1)
            {
                throw new InvalidOperationException("List rankings are too large to reorder safely.");
            }

            return maxRanking + 1;
        }

        private static void ValidateRanking(int ranking)
        {
            if (ranking <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ranking), "Ranking must be positive.");
            }
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
