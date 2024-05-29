using EatMyMovies.DataAccess.Models;

namespace EatMyMoviesSite.Services
{
	public interface IStorageService
	{
		void ShuffleListDownIfNecessary(List list, int ranking);
	}
}