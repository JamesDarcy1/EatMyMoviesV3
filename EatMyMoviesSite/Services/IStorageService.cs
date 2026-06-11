namespace EatMyMoviesSite.Services
{
	public interface IStorageService
	{
		Task ShuffleListDownIfNecessaryAsync(Guid listId, int ranking, CancellationToken cancellationToken = default);
	}
}
