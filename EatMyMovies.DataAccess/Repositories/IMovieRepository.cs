using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
        List<Genre> GetAllGenres();
        IEnumerable<Movie> GetAllMovies();
        Movie GetMovieByTitle(string title);
        List<Movie> GetMoviesByGenre(string genre);
        void SaveGenres(Movie movie, IEnumerable<string> genres);
        Movie SaveTmdbMovie(string title, int tmdbId, decimal imdbRating);
    }
}