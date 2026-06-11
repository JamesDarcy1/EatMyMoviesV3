namespace EatMyMoviesSite.Services
{
	public interface IStorageService
	{
		Task AddMovieToListAtRankingAsync(Guid movieId, Guid listId, int ranking, CancellationToken cancellationToken = default);
        Task MoveMovieWithinListAsync(Guid movieId, Guid listId, int newRanking, CancellationToken cancellationToken = default);
	}
}
