using EatMyMoviesSite.Controllers;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EatMyMovies.Tests;

public class MovieControllerTests
{
    [Fact]
    public void SpinTheWheel_ReturnsExpectedView()
    {
        var movieService = new Mock<IMovieService>();
        var logger = new Mock<ILogger<MovieController>>();
        var controller = new MovieController(movieService.Object, logger.Object);

        var result = controller.SpinTheWheel();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/Movie/SpinTheWheel.cshtml", viewResult.ViewName);
        movieService.VerifyNoOtherCalls();
    }
}
