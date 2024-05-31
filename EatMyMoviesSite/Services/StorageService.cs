using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;

namespace EatMyMoviesSite.Services
{
	public class StorageService : IStorageService
	{
		private IRankingRepository _rankingRepository;
		public StorageService(IRankingRepository rankingRepository)
		{
			_rankingRepository = rankingRepository;
		}

		public void ShuffleListDownIfNecessary(List list, int newRanking)
		{
			var listRanking = _rankingRepository.GetMovieAtRanking(list, newRanking);
			if (listRanking is not null)
			{
				var moviesInList = _rankingRepository.GetAllRankingsInList(list);
				foreach (var movie in moviesInList)
				{
					if (movie.Ranking >= newRanking)
					{
						_rankingRepository.UpdateRanking(movie, movie.Ranking+1);
					}
				}

			}
		}
	}
}
