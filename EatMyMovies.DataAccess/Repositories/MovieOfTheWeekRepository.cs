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

        public async Task SetSelectionAsync(Guid movieId, string? editorialNote, CancellationToken cancellationToken = default)
        {
            if (movieId == Guid.Empty)
            {
                throw new ArgumentException("Movie id cannot be empty.", nameof(movieId));
            }
            editorialNote = NormalizeEditorialNote(editorialNote);

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
                    EditorialNote = editorialNote,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                selection.MovieId = movieId;
                selection.EditorialNote = editorialNote;
                selection.UpdatedUtc = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateEditorialNoteAsync(string? editorialNote, CancellationToken cancellationToken = default)
        {
            editorialNote = NormalizeEditorialNote(editorialNote);
            var selection = await _dbContext.MovieOfTheWeekSelections
                .FirstOrDefaultAsync(current => current.MovieOfTheWeekSelectionId == MovieOfTheWeekSelection.SingletonId, cancellationToken)
                ?? throw new InvalidOperationException("No Movie of the Week is selected.");
            selection.EditorialNote = editorialNote;
            selection.UpdatedUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string? NormalizeEditorialNote(string? note)
        {
            var result = note?.Trim() ?? string.Empty;
            if (result.Length == 0) return null;
            if (result.Length is < 20 or > 1500)
                throw new ArgumentException("When provided, the editorial note must be between 20 and 1500 characters.", nameof(note));
            return result;
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
