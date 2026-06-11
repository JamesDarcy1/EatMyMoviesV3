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

		public Task AddMovieToListAtRankingAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default)
		{
			return _rankingRepository.AddMovieToListAtRankingAsync(movieId, listId, ranking, cancellationToken);
		}

		public Task MoveMovieWithinListAsync(Guid movieId, Guid listId, int newRanking, CancellationToken cancellationToken = default)
		{
			return _rankingRepository.MoveMovieWithinListAsync(movieId, listId, newRanking, cancellationToken);
		}
	}
}
