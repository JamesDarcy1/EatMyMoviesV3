using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMoviesSite.Services
{
	public interface IMovieService
	{
		Task<MovieList> BuildMovieList(string listTitle, int page);
        List<EatMyMovies.DataAccess.Models.Genre> GetAllGenres();
        Task<decimal?> GetImdbRating(string movieTitle);
		Task<Movie> GetMovieByTitle(string title);
		Task<Movie> GetMoviesById(int id);
        Task<List<MovieDropdown>> SearchMoviesByTitle(string titleSearch);
        Task<Video> GetTrailer(int movieId);
        IList<T> ShuffleList<T>(IList<T> list);
        Task<List<Movie>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange);
        Task<Person> GetDirector(int movieId);
        Task<List<Movie>> GetFastRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange);
        Task<List<Person>> GetActors(int movieId);
        List<MovieRanking> GetListRankingsForMovie(Guid movieId);
        EatMyMovies.DataAccess.Models.Movie GetStoreMovieByTitle(string title);
    }
}