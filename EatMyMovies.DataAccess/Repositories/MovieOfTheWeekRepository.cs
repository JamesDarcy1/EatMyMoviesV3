using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess.Repositories
{
    public sealed class MovieOfTheWeekRepository : IMovieOfTheWeekRepository
    {
        private readonly EatMyMoviesContext _dbContext;

        public MovieOfTheWeekRepository(EatMyMoviesContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<MovieOfTheWeekSelection?> GetSelectionAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.MovieOfTheWeekSelections
                .AsNoTracking()
                .Include(selection => selection.Movie)
                .FirstOrDefaultAsync(
                    selection => selection.MovieOfTheWeekSelectionId == MovieOfTheWeekSelection.SingletonId,
                    cancellationToken);
        }

        public async Task SetSelectionAsync(Guid movieId, CancellationToken cancellationToken = default)
        {
            if (movieId == Guid.Empty)
            {
                throw new ArgumentException("Movie id cannot be empty.", nameof(movieId));
            }

            var movieExists = await _dbContext.Movies
                .AsNoTracking()
                .AnyAsync(movie => movie.MovieId == movieId, cancellationToken);

            if (!movieExists)
            {
                throw new InvalidOperationException("Movie was not found.");
            }

            var selection = await _dbContext.MovieOfTheWeekSelections
                .FirstOrDefaultAsync(
                    currentSelection => currentSelection.MovieOfTheWeekSelectionId == MovieOfTheWeekSelection.SingletonId,
                    cancellationToken);

            if (selection == null)
            {
                _dbContext.MovieOfTheWeekSelections.Add(new MovieOfTheWeekSelection
                {
                    MovieOfTheWeekSelectionId = MovieOfTheWeekSelection.SingletonId,
                    MovieId = movieId,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                selection.MovieId = movieId;
                selection.UpdatedUtc = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ClearSelectionAsync(CancellationToken cancellationToken = default)
        {
            var selection = await _dbContext.MovieOfTheWeekSelections
                .FirstOrDefaultAsync(
                    currentSelection => currentSelection.MovieOfTheWeekSelectionId == MovieOfTheWeekSelection.SingletonId,
                    cancellationToken);

            if (selection == null)
            {
                return;
            }

            _dbContext.MovieOfTheWeekSelections.Remove(selection);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
