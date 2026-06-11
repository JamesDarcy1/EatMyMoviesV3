using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Moq;

namespace EatMyMovies.Tests;

public class StorageServiceTests
{
    [Fact]
    public async Task ShuffleListDownIfNecessaryAsync_DoesNothingWhenRankingIsEmpty()
    {
        var list = TestHelpers.CreateList();
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetMovieAtRankingAsync(list.ListId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListRanking)null!);
        var service = new StorageService(rankingRepository.Object);

        await service.ShuffleListDownIfNecessaryAsync(list.ListId, 2);

        rankingRepository.Verify(repository => repository.GetRankingsAtOrAfterAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        rankingRepository.Verify(repository => repository.UpdateRankingAsync(It.IsAny<ListRanking>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShuffleListDownIfNecessaryAsync_IncrementsRankingsAtAndAfterCollision()
    {
        var list = TestHelpers.CreateList();
        var first = new ListRanking { ListId = list.ListId, List = list, Movie = TestHelpers.CreateStoreMovie("First"), Ranking = 1 };
        var second = new ListRanking { ListId = list.ListId, List = list, Movie = TestHelpers.CreateStoreMovie("Second"), Ranking = 2 };
        var third = new ListRanking { ListId = list.ListId, List = list, Movie = TestHelpers.CreateStoreMovie("Third"), Ranking = 3 };
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetMovieAtRankingAsync(list.ListId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(second);
        rankingRepository.Setup(repository => repository.GetRankingsAtOrAfterAsync(list.ListId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ListRanking> { second, third });
        rankingRepository.Setup(repository => repository.UpdateRankingAsync(It.IsAny<ListRanking>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListRanking ranking, int _, CancellationToken _) => ranking);
        var service = new StorageService(rankingRepository.Object);

        await service.ShuffleListDownIfNecessaryAsync(list.ListId, 2);

        rankingRepository.Verify(repository => repository.UpdateRankingAsync(first, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        rankingRepository.Verify(repository => repository.UpdateRankingAsync(second, 3, It.IsAny<CancellationToken>()), Times.Once);
        rankingRepository.Verify(repository => repository.UpdateRankingAsync(third, 4, It.IsAny<CancellationToken>()), Times.Once);
    }
}
