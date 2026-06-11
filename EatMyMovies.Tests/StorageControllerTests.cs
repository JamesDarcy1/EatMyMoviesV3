using DataList = EatMyMovies.DataAccess.Models.List;
using DataMovie = EatMyMovies.DataAccess.Models.Movie;
using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Controllers;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EatMyMovies.Tests;

public class StorageControllerTests
{
    [Fact]
    public async Task SaveToDatabase_ThrowsWhenMovieAlreadyExists()
    {
        var movieRepository = new Mock<IMovieRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateStoreMovie("Existing"));
        var controller = CreateController(movieRepository: movieRepository);

        var exception = await Assert.ThrowsAsync<Exception>(() => controller.SaveToDatabase("Existing"));

        Assert.Equal("Existing already in db", exception.Message);
    }

    [Fact]
    public async Task SaveToDatabase_SavesMovieAndGenresWhenTitleIsNew()
    {
        var tmdbMovie = TestHelpers.CreateTmdbMovie(title: "New Movie", genres: new[] { "Drama", "Thriller" });
        var savedMovie = TestHelpers.CreateStoreMovie("New Movie", tmdbMovie.Id);
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("New Movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataMovie)null!);
        movieRepository.Setup(repository => repository.SaveTmdbMovieAsync("New Movie", tmdbMovie.Id, 8.4m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMovie);
        movieService.Setup(service => service.GetMovieByTitle("New Movie"))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("New Movie"))
            .ReturnsAsync(8.4m);
        var controller = CreateController(movieRepository: movieRepository, movieService: movieService);

        var result = await controller.SaveToDatabase("New Movie");

        Assert.Same(savedMovie, result);
        movieRepository.Verify(repository => repository.SaveGenresAsync(
            savedMovie.MovieId,
            It.Is<IEnumerable<string>>(genres => genres.SequenceEqual(new[] { "Drama", "Thriller" })),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddMovieToList_RedirectsWithoutInsertWhenMovieAlreadyInList()
    {
        var movie = TestHelpers.CreateStoreMovie("Existing Movie");
        var list = TestHelpers.CreateList("Top 100");
        var movieRepository = new Mock<IMovieRepository>();
        var listRepository = new Mock<IListRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("Existing Movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        listRepository.Setup(repository => repository.GetListByNameAsync("Top 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(movie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = CreateController(
            movieRepository: movieRepository,
            listRepository: listRepository,
            rankingRepository: rankingRepository);
        SetReferer(controller);

        var result = await controller.AddMovieToList("Top 100", "Existing Movie", 5);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/previous-page", redirect.Url);
        rankingRepository.Verify(repository => repository.InsertMovieToListAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddMovieToList_SavesMissingMovieShufflesOccupiedRankingAndInserts()
    {
        var list = TestHelpers.CreateList("Top 100");
        var tmdbMovie = TestHelpers.CreateTmdbMovie(title: "New Movie", genres: new[] { "Comedy" });
        var savedMovie = TestHelpers.CreateStoreMovie("New Movie", tmdbMovie.Id);
        var insertedRanking = new ListRanking
        {
            ListId = list.ListId,
            MovieId = savedMovie.MovieId,
            Ranking = 2
        };
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var listRepository = new Mock<IListRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        var storageService = new Mock<IStorageService>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("New Movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataMovie)null!);
        movieRepository.Setup(repository => repository.SaveTmdbMovieAsync("New Movie", tmdbMovie.Id, 7.2m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMovie);
        movieService.Setup(service => service.GetMovieByTitle("New Movie"))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("New Movie"))
            .ReturnsAsync(7.2m);
        listRepository.Setup(repository => repository.GetListByNameAsync("Top 100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(savedMovie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        rankingRepository.Setup(repository => repository.InsertMovieToListAsync(savedMovie.MovieId, list.ListId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(insertedRanking);
        var controller = CreateController(
            movieRepository,
            movieService,
            listRepository,
            rankingRepository,
            storageService);
        SetReferer(controller);

        var result = await controller.AddMovieToList("Top 100", "New Movie", 2);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/previous-page", redirect.Url);
        storageService.Verify(service => service.ShuffleListDownIfNecessaryAsync(list.ListId, 2, It.IsAny<CancellationToken>()), Times.Once);
        rankingRepository.Verify(repository => repository.InsertMovieToListAsync(savedMovie.MovieId, list.ListId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMovieToList_ThrowsWhenListDoesNotExist()
    {
        var movieRepository = new Mock<IMovieRepository>();
        var listRepository = new Mock<IListRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTitleAsync("Movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.CreateStoreMovie("Movie"));
        listRepository.Setup(repository => repository.GetListByNameAsync("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataList)null!);
        var controller = CreateController(movieRepository: movieRepository, listRepository: listRepository);

        var exception = await Assert.ThrowsAsync<Exception>(() => controller.AddMovieToList("Missing", "Movie", 1));

        Assert.Equal("List 'Missing' was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateRanking_ReturnsNotFoundWhenRankingDoesNotExist()
    {
        var movieId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetListRankingAsync(movieId, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListRanking)null!);
        var controller = CreateController(rankingRepository: rankingRepository);

        var result = await controller.UpdateRanking(movieId, listId, 10);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ranking not found", notFound.Value);
    }

    [Fact]
    public async Task DeleteRanking_RemovesRankingAndRedirects()
    {
        var movieId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var rankingRepository = new Mock<IRankingRepository>();
        var controller = CreateController(rankingRepository: rankingRepository);
        SetReferer(controller);

        var result = await controller.DeleteRanking(movieId, listId);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/previous-page", redirect.Url);
        rankingRepository.Verify(repository => repository.RemoveListRankingAsync(movieId, listId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static StorageController CreateController(
        Mock<IMovieRepository>? movieRepository = null,
        Mock<IMovieService>? movieService = null,
        Mock<IListRepository>? listRepository = null,
        Mock<IRankingRepository>? rankingRepository = null,
        Mock<IStorageService>? storageService = null)
    {
        return new StorageController(
            (movieRepository ?? new Mock<IMovieRepository>()).Object,
            (movieService ?? new Mock<IMovieService>()).Object,
            (listRepository ?? new Mock<IListRepository>()).Object,
            (rankingRepository ?? new Mock<IRankingRepository>()).Object,
            (storageService ?? new Mock<IStorageService>()).Object);
    }

    private static void SetReferer(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers.Referer = "/previous-page";
    }
}
