using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IRankingRepository
	{
		IEnumerable<ListRanking> GetAllRankingsInList(List list);
		ListRanking GetMovieAtRanking(List list, int ranking);
		IEnumerable<Movie> GetMoviesForList(string listName);
		int GetRankingOfMovie(Guid movieId, string listName);
		ListRanking InsertMovieToList(Movie movie, List list, int ranking);
		ListRanking UpdateRanking(ListRanking listRanking, int newRanking);
	}
}