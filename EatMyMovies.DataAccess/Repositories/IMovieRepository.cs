using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
		Movie GetMovieByTitle(string title);
		Movie SaveTmdbMovie(string title, int tmdbId, decimal imdbRating);
	}
}