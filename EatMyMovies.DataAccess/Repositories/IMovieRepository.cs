using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
        IEnumerable<Movie> GetAllMovies();
        Movie GetMovieByTitle(string title);
        void SaveGenres(Movie movie, IEnumerable<string> genres);
        Movie SaveTmdbMovie(string title, int tmdbId, decimal imdbRating);
    }
}