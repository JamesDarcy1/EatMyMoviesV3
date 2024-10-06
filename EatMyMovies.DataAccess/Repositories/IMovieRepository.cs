using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IMovieRepository
	{
        List<Genre> GetAllGenres();
        IEnumerable<Movie> GetAllMovies();
        IQueryable<string> GetGenresOfMovie(Movie movie);
        Movie GetMovieByTitle(string title);
        List<Movie> GetMoviesOfGenres(List<string> genres);
        void SaveGenres(Movie movie, IEnumerable<string> genres);
        Movie SaveTmdbMovie(string title, int tmdbId, decimal? imdbRating);
    }
}