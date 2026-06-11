using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Moq;

namespace EatMyMovies.Tests;

public class StorageServiceTests
{
    [Fact]
    public async Task AddMovieToListAtRankingAsync_DelegatesToRankingRepository()
    {
        var list = TestHelpers.CreateList();
        var movie = TestHelpers.CreateStoreMovie();
        var rankingRepository = new Mock<IRankingRepository>();
        var service = new StorageService(rankingRepository.Object);

        await service.AddMovieToListAtRankingAsync(movie.MovieId, list.ListId, 2);

        rankingRepository.Verify(repository => repository.AddMovieToListAtRankingAsync(movie.MovieId, list.ListId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveMovieWithinListAsync_DelegatesToRankingRepository()
    {
        var list = TestHelpers.CreateList();
        var movie = TestHelpers.CreateStoreMovie();
        var rankingRepository = new Mock<IRankingRepository>();
        var service = new StorageService(rankingRepository.Object);

        await service.MoveMovieWithinListAsync(movie.MovieId, list.ListId, 2);

        rankingRepository.Verify(repository => repository.MoveMovieWithinListAsync(movie.MovieId, list.ListId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
