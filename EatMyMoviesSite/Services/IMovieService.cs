using EatMyMoviesSite.DTOs;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite.Services
{
	public interface IMovieService
	{
		Task<MovieList> BuildMovieList(string listTitle, int page);
		Task<decimal> GetImdbRating(string movieTitle);
		Task<Movie> GetMovieByTitle(string title);
		Task<Movie> GetMoviesById(int id);
		Task<Video> GetTrailer(int movieId);
	}
}