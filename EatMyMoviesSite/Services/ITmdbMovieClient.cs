using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TmdbPerson = TMDbLib.Objects.People.Person;

namespace EatMyMoviesSite.Services
{
    internal interface ITmdbMovieClient
    {
        Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title);

        Task<SearchContainer<SearchMovie>> SearchMoviesAsync(string title, int page);

        Task<Movie> GetMovieByIdAsync(int id);

        Task<ResultContainer<Video>> GetMovieVideosAsync(int movieId);

        Task<Credits> GetMovieCreditsAsync(int movieId);

        Task<TmdbPerson?> GetPersonAsync(int personId);
    }
}
