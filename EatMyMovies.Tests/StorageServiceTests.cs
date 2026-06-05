using DataList = EatMyMovies.DataAccess.Models.List;
using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Moq;

namespace EatMyMovies.Tests;

public class StorageServiceTests
{
    [Fact]
    public void ShuffleListDownIfNecessary_DoesNothingWhenRankingIsEmpty()
    {
        var list = TestHelpers.CreateList();
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetMovieAtRanking(list, 2))
            .Returns((ListRanking)null!);
        var service = new StorageService(rankingRepository.Object);

        service.ShuffleListDownIfNecessary(list, 2);

        rankingRepository.Verify(repository => repository.GetAllRankingsInList(It.IsAny<DataList>()), Times.Never);
        rankingRepository.Verify(repository => repository.UpdateRanking(It.IsAny<ListRanking>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ShuffleListDownIfNecessary_IncrementsRankingsAtAndAfterCollision()
    {
        var list = TestHelpers.CreateList();
        var first = new ListRanking { List = list, Movie = TestHelpers.CreateStoreMovie("First"), Ranking = 1 };
        var second = new ListRanking { List = list, Movie = TestHelpers.CreateStoreMovie("Second"), Ranking = 2 };
        var third = new ListRanking { List = list, Movie = TestHelpers.CreateStoreMovie("Third"), Ranking = 3 };
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetMovieAtRanking(list, 2))
            .Returns(second);
        rankingRepository.Setup(repository => repository.GetAllRankingsInList(list))
            .Returns(new[] { first, second, third });
        var service = new StorageService(rankingRepository.Object);

        service.ShuffleListDownIfNecessary(list, 2);

        rankingRepository.Verify(repository => repository.UpdateRanking(first, It.IsAny<int>()), Times.Never);
        rankingRepository.Verify(repository => repository.UpdateRanking(second, 3), Times.Once);
        rankingRepository.Verify(repository => repository.UpdateRanking(third, 4), Times.Once);
    }
}
