using EatMyMoviesSite.DTOs;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite.Services
{
	public interface IMovieService
	{
		Task<MovieList> BuildMovieList(string listTitle, int page);
        List<EatMyMovies.DataAccess.Models.Genre> GetAllGenres();
        Task<decimal> GetImdbRating(string movieTitle);
		Task<Movie> GetMovieByTitle(string title);
		Task<Movie> GetMoviesById(int id);
        EatMyMovies.DataAccess.Models.Movie GetRecommendationByGenre(string genre);
        Task<Video> GetTrailer(int movieId);
        IList<T> ShuffleList<T>(IList<T> list);
    }
}