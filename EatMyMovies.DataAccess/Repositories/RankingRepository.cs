using EatMyMovies.DataAccess.Models;
using System.Collections.Generic;

namespace EatMyMovies.DataAccess.Repositories
{
	public class RankingRepository : IRankingRepository
	{
		private readonly EatMyMoviesContext _dbContext;

		public RankingRepository(EatMyMoviesContext dbContext)
		{
			_dbContext = dbContext;
		}

		public IEnumerable<Movie> GetMoviesForList(string listName)
		{
			var movies = _dbContext.ListRankings.Where(l => l.List.Name == listName)
												.OrderBy(m => m.Ranking)
												.Select(m => m.Movie).ToList();
			return movies;
		}

		public int GetRankingOfMovie(Guid movieId, string listName)
		{
			var listRanking = _dbContext.ListRankings.FirstOrDefault(r => r.List.Name == listName && r.Movie.MovieId == movieId);
			if(listRanking == null)
			{
				throw new Exception("Film does not exist in list");
			}
			return listRanking.Ranking;
		}

		public ListRanking InsertMovieToList(Movie movie, List list, int ranking)
		{
			var result = _dbContext.Add(new ListRanking()
			{
				List = list,
				Movie = movie,
				Ranking = ranking
			});

			_dbContext.SaveChanges();

			return result.Entity;
		}

		public ListRanking GetMovieAtRanking(List list, int ranking)
		{
			var listRanking = _dbContext.ListRankings.FirstOrDefault(lr => lr.List == list && lr.Ranking == ranking);
			return listRanking;
		}

		public bool FilmExistsInList(Guid movieId, Guid listId)
		{
			return _dbContext.ListRankings.Any(lr => lr.Movie.MovieId == movieId && lr.List.ListId == listId);
		}

		public IEnumerable<ListRanking> GetAllRankingsInList(List list)
		{
			var listRankings = _dbContext.ListRankings.Where(lr => lr.List == list)
													  .OrderBy(lr => lr.Ranking).ToList();
			return listRankings;
		}

		public ListRanking UpdateRanking(ListRanking listRanking, int newRanking)
		{
			listRanking.Ranking = newRanking;
			var updatedListRanking = _dbContext.ListRankings.Update(listRanking);
			_dbContext.SaveChanges();

			return updatedListRanking.Entity;
		}
	}
}
