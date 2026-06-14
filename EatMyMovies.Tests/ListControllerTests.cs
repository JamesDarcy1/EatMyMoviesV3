using EatMyMoviesSite.Controllers;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EatMyMovies.Tests;

public class ListControllerTests
{
    [Theory]
    [InlineData("Top 100", "~/Views/list/list.cshtml")]
    [InlineData("Comedies", "~/Views/List/List.cshtml")]
    [InlineData("Standout Soundtracks", "~/Views/List/List.cshtml")]
    public async Task ListActions_BuildExpectedListAndReturnConfiguredView(string listName, string expectedView)
    {
        var movieList = new MovieList
        {
            Name = listName,
            Description = "A list",
            Movies = new List<ListMovie>()
        };
        var movieService = new Mock<IMovieService>();
        movieService.Setup(service => service.BuildMovieList(listName, 3))
            .ReturnsAsync(movieList);
        var controller = new ListController(movieService.Object);

        var result = listName switch
        {
            "Top 100" => await controller.Top100(3),
            "Comedies" => await controller.Comedies(3),
            "Standout Soundtracks" => await controller.StandoutSoundtracks(3),
            _ => throw new InvalidOperationException("Unexpected test case")
        };

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedView, viewResult.ViewName);
        Assert.Same(movieList, viewResult.Model);
        movieService.Verify(service => service.BuildMovieList(listName, 3), Times.Once);
    }

    [Fact]
    public void LegacyTop100_RedirectsPermanentlyToCanonicalActionAndPreservesPage()
    {
        var movieService = new Mock<IMovieService>();
        var controller = new ListController(movieService.Object);

        var result = controller.LegacyTop100(4);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.True(redirect.Permanent);
        Assert.Equal(nameof(ListController.Top100), redirect.ActionName);
        Assert.Equal(4, redirect.RouteValues?["page"]);
        movieService.VerifyNoOtherCalls();
    }
}
