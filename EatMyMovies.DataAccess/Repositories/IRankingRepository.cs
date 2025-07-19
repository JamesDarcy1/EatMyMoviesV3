using EatMyMovies.DataAccess.Models;

namespace EatMyMovies.DataAccess.Repositories
{
	public interface IRankingRepository
	{
		bool FilmExistsInList(Guid movieId, Guid listId);
        IQueryable<Movie> GetAllMoviesInList(string listName);
        IEnumerable<ListRanking> GetAllRankingsInList(List list);
        int GetListCount(string listName);
        List<ListRanking> GetListRankingsForMovie(Guid movieId);
        ListRanking GetMovieAtRanking(List list, int ranking);
		IEnumerable<Movie> GetMoviesForListByPage(string listName, int page = 1);
		int GetRankingOfMovie(Guid movieId, string listName);
		ListRanking InsertMovieToList(Movie movie, List list, int ranking);
		void RemoveRanking(int ranking, Guid listId);
		ListRanking UpdateRanking(ListRanking listRanking, int newRanking);
	}
}