using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.QueryModels;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess.Repositories
{
	public class MovieRepository : IMovieRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public MovieRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<Movie> SaveTmdbMovieAsync(string title, int tmdbId, decimal? imdbRating, CancellationToken cancellationToken = default)
		{
            var normalizedTitle = NormalizeRequired(title, nameof(title));
            if (tmdbId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tmdbId), "TMDb ID must be positive.");
            }

			var movie = _dbContext.Movies.Add(new Movie()
			{
				Title = normalizedTitle,
				TmdbId = tmdbId,
			});

			await _dbContext.SaveChangesAsync(cancellationToken);

			return movie.Entity;
		}

        public Task<Movie?> GetMovieByIdAsync(Guid movieId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.MovieId == movieId, cancellationToken);
        }

        public Task<Movie?> GetMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            if (tmdbId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tmdbId), "TMDb ID must be positive.");
            }

            return _dbContext.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(movie => movie.TmdbId == tmdbId, cancellationToken);
        }

		public Task<Movie?> GetMovieByTitleAsync(string title, CancellationToken cancellationToken = default)
		{
            var normalizedTitle = NormalizeRequired(title, nameof(title));

			return _dbContext.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Title == normalizedTitle, cancellationToken);
		}

        public Task<List<StoredMovieSummary>> GetAllMovieSummariesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Movies
                .AsNoTracking()
                .Select(movie => new StoredMovieSummary(movie.MovieId, movie.Title, movie.TmdbId))
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountMoviesAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            return ApplyMovieSearch(_dbContext.Movies.AsNoTracking(), searchTerm)
                .CountAsync(cancellationToken);
        }

        public Task<List<AdminMovieSummary>> SearchMoviesAsync(
            string? searchTerm,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            return ApplyMovieSearch(_dbContext.Movies.AsNoTracking(), searchTerm)
                .OrderBy(movie => movie.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(movie => new AdminMovieSummary(
                    movie.MovieId,
                    movie.Title,
                    movie.TmdbId,
                    movie.ListRankings.Count))
                .ToListAsync(cancellationToken);
        }

        public async Task SaveGenresAsync(Guid movieId, IEnumerable<string> genres, CancellationToken cancellationToken = default)
        {
            var genreNames = genres
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Select(genre => genre.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (genreNames.Count == 0)
            {
                return;
            }

			var existingGenres = await _dbContext.Genres
                .Where(genre => genreNames.Contains(genre.Name))
                .ToListAsync(cancellationToken);

            var existingByName = existingGenres.ToDictionary(genre => genre.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var genreName in genreNames)
            {
                if (!existingByName.ContainsKey(genreName))
                {
                    var genre = new Genre
                    {
                        GenreId = Guid.NewGuid(),
                        Name = genreName
                    };
                    existingByName.Add(genreName, genre);
                    _dbContext.Genres.Add(genre);
                }
            }

            var existingLinks = await _dbContext.MovieGenres
                .Where(movieGenre => movieGenre.MovieId == movieId && genreNames.Contains(movieGenre.Genre.Name))
                .Select(movieGenre => movieGenre.Genre.Name)
                .ToListAsync(cancellationToken);
            var existingLinkNames = existingLinks.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var genreName in genreNames)
            {
                if (existingLinkNames.Contains(genreName))
                {
                    continue;
                }

                _dbContext.MovieGenres.Add(new MovieGenre
                {
                    MovieId = movieId,
                    GenreId = existingByName[genreName].GenreId
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

		public Task<List<Genre>> GetAllGenresAsync(CancellationToken cancellationToken = default)
		{
			return _dbContext.Genres
                .AsNoTracking()
                .OrderBy(genre => genre.Name)
                .ToListAsync(cancellationToken);
		}

		public Task<List<StoredMovieSummary>> GetMoviesOfGenresAsync(IReadOnlyCollection<string> genres, CancellationToken cancellationToken = default)
		{
            if (genres.Count == 0)
            {
                return Task.FromResult(new List<StoredMovieSummary>());
            }

			return _dbContext.MovieGenres
                .AsNoTracking()
                .Where(movieGenre => genres.Contains(movieGenre.Genre.Name))
                .Select(movieGenre => new StoredMovieSummary(movieGenre.MovieId, movieGenre.Movie.Title, movieGenre.Movie.TmdbId))
                .Distinct()
                .ToListAsync(cancellationToken);
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

        private static IQueryable<Movie> ApplyMovieSearch(IQueryable<Movie> query, string? searchTerm)
        {
            var normalizedSearchTerm = searchTerm?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                return query;
            }

            return query.Where(movie => movie.Title.Contains(normalizedSearchTerm));
        }
    }
}
