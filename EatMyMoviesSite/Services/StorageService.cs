using EatMyMovies.DataAccess.Repositories;

namespace EatMyMoviesSite.Services
{
	public class StorageService : IStorageService
	{
		private readonly IRankingRepository _rankingRepository;

		public StorageService(IRankingRepository rankingRepository)
		{
			_rankingRepository = rankingRepository;
		}

		public async Task ShuffleListDownIfNecessaryAsync(Guid listId, int newRanking, CancellationToken cancellationToken = default)
		{
			var listRanking = await _rankingRepository.GetMovieAtRankingAsync(listId, newRanking, cancellationToken);
			if (listRanking is null)
			{
				return;
			}

			var moviesInList = await _rankingRepository.GetRankingsAtOrAfterAsync(listId, newRanking, cancellationToken);
			foreach (var movie in moviesInList)
			{
				await _rankingRepository.UpdateRankingAsync(movie, movie.Ranking + 1, cancellationToken);
			}
		}
	}
}
