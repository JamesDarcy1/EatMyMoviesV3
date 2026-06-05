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
        movieRepository.Setup(repository => repository.GetMovieByTitle("Existing"))
            .Returns(TestHelpers.CreateStoreMovie("Existing"));
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
        movieRepository.Setup(repository => repository.GetMovieByTitle("New Movie"))
            .Returns((DataMovie)null!);
        movieRepository.Setup(repository => repository.SaveTmdbMovie("New Movie", tmdbMovie.Id, 8.4m))
            .Returns(savedMovie);
        movieService.Setup(service => service.GetMovieByTitle("New Movie"))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("New Movie"))
            .ReturnsAsync(8.4m);
        var controller = CreateController(movieRepository: movieRepository, movieService: movieService);

        var result = await controller.SaveToDatabase("New Movie");

        Assert.Same(savedMovie, result);
        movieRepository.Verify(repository => repository.SaveGenres(
            savedMovie,
            It.Is<IEnumerable<string>>(genres => genres.SequenceEqual(new[] { "Drama", "Thriller" }))),
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
        movieRepository.Setup(repository => repository.GetMovieByTitle("Existing Movie"))
            .Returns(movie);
        listRepository.Setup(repository => repository.GetListByName("Top 100"))
            .Returns(list);
        rankingRepository.Setup(repository => repository.FilmExistsInList(movie.MovieId, list.ListId))
            .Returns(true);
        var controller = CreateController(
            movieRepository: movieRepository,
            listRepository: listRepository,
            rankingRepository: rankingRepository);
        SetReferer(controller);

        var result = await controller.AddMovieToList("Top 100", "Existing Movie", 5);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/previous-page", redirect.Url);
        rankingRepository.Verify(repository => repository.InsertMovieToList(
            It.IsAny<DataMovie>(),
            It.IsAny<DataList>(),
            It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddMovieToList_SavesMissingMovieShufflesOccupiedRankingAndInserts()
    {
        var list = TestHelpers.CreateList("Top 100");
        var tmdbMovie = TestHelpers.CreateTmdbMovie(title: "New Movie", genres: new[] { "Comedy" });
        var savedMovie = TestHelpers.CreateStoreMovie("New Movie", tmdbMovie.Id);
        var existingRanking = new ListRanking
        {
            List = list,
            Movie = TestHelpers.CreateStoreMovie("Other Movie"),
            Ranking = 2
        };
        var insertedRanking = new ListRanking
        {
            List = list,
            Movie = savedMovie,
            Ranking = 2
        };
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var listRepository = new Mock<IListRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        var storageService = new Mock<IStorageService>();
        movieRepository.Setup(repository => repository.GetMovieByTitle("New Movie"))
            .Returns((DataMovie)null!);
        movieRepository.Setup(repository => repository.SaveTmdbMovie("New Movie", tmdbMovie.Id, 7.2m))
            .Returns(savedMovie);
        movieService.Setup(service => service.GetMovieByTitle("New Movie"))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("New Movie"))
            .ReturnsAsync(7.2m);
        listRepository.Setup(repository => repository.GetListByName("Top 100"))
            .Returns(list);
        rankingRepository.Setup(repository => repository.FilmExistsInList(savedMovie.MovieId, list.ListId))
            .Returns(false);
        rankingRepository.Setup(repository => repository.GetAllRankingsInList(list))
            .Returns(new[] { existingRanking });
        rankingRepository.Setup(repository => repository.InsertMovieToList(savedMovie, list, 2))
            .Returns(insertedRanking);
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
        storageService.Verify(service => service.ShuffleListDownIfNecessary(list, 2), Times.Once);
        rankingRepository.Verify(repository => repository.InsertMovieToList(savedMovie, list, 2), Times.Once);
    }

    [Fact]
    public void UpdateRanking_ReturnsNotFoundWhenRankingDoesNotExist()
    {
        var movieId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var rankingRepository = new Mock<IRankingRepository>();
        rankingRepository.Setup(repository => repository.GetListRanking(movieId, listId))
            .Returns((ListRanking)null!);
        var controller = CreateController(rankingRepository: rankingRepository);

        var result = controller.UpdateRanking(movieId, listId, 10);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ranking not found", notFound.Value);
    }

    [Fact]
    public void DeleteRanking_RemovesRankingAndRedirects()
    {
        var movieId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var rankingRepository = new Mock<IRankingRepository>();
        var controller = CreateController(rankingRepository: rankingRepository);
        SetReferer(controller);

        var result = controller.DeleteRanking(movieId, listId);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/previous-page", redirect.Url);
        rankingRepository.Verify(repository => repository.RemoveListRanking(movieId, listId), Times.Once);
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
