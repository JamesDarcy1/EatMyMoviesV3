using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public class MovieRepository : IMovieRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public MovieRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Movie SaveTmdbMovie(string title, int tmdbId, decimal imdbRating)
		{
			var result = _dbContext.Movies.Add(new Movie()
			{
				Title = title,
				TmdbId = tmdbId,
				ImdbRating = imdbRating
			});

			_dbContext.SaveChanges();

			return result.Entity;
		}

		public Movie GetMovieByTitle(string title)
		{
			var movie = _dbContext.Movies.FirstOrDefault(x => x.Title == title);
			return movie;
		}
	}
}
