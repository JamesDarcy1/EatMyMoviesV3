using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using DataGenre = EatMyMovies.DataAccess.Models.Genre;
using DataList = EatMyMovies.DataAccess.Models.List;
using DataMovie = EatMyMovies.DataAccess.Models.Movie;

namespace EatMyMoviesSite.Services
{
	public interface IMovieService
	{
		Task<MovieList> BuildMovieList(string listTitle, int page, CancellationToken cancellationToken = default);
        Task<MovieDetail> BuildMovieDetail(string? title, int? tmdbId, bool includeListContext, CancellationToken cancellationToken = default);
        Task<List<DataGenre>> GetAllGenresAsync(CancellationToken cancellationToken = default);
        Task<decimal?> GetImdbRating(string movieTitle);
		Task<Movie> GetMovieByTitle(string title);
		Task<Movie> GetMovieById(int id);
        Task<List<MovieDropdown>> SearchMoviesByTitle(string titleSearch);
        Task<Video?> GetTrailer(int movieId);
        IList<T> ShuffleList<T>(IList<T> list);
        Task<List<Movie>> GetRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange, CancellationToken cancellationToken = default);
        Task<Person> GetDirector(int movieId);
        Task<List<Movie>> GetFastRecommendations(string feelings, string duration, bool openToForeignFilm, string yearRange, CancellationToken cancellationToken = default);
        Task<List<Person>> GetActors(int movieId);
        Task<List<MovieRanking>> GetListRankingsForMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
        Task<DataMovie?> GetStoreMovieByTitleAsync(string title, CancellationToken cancellationToken = default);
        Task<List<DataList>> GetAllListsAsync(CancellationToken cancellationToken = default);
    }
}
