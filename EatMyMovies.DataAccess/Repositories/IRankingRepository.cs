using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IRankingRepository
	{
		bool FilmExistsInList(Guid movieId, Guid listId);
		IEnumerable<ListRanking> GetAllRankingsInList(List list);
		ListRanking GetMovieAtRanking(List list, int ranking);
		IEnumerable<Movie> GetMoviesForList(string listName);
		int GetRankingOfMovie(Guid movieId, string listName);
		ListRanking InsertMovieToList(Movie movie, List list, int ranking);
		void RemoveRanking(int ranking, Guid listId);
		ListRanking UpdateRanking(ListRanking listRanking, int newRanking);
	}
}