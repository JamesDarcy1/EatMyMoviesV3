using System.ComponentModel.DataAnnotations;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace EatMyMovies.Tests;

public class SeoMetadataTests
{
    [Fact]
    public async Task HostRedirect_DoesNotLoop_WhenConfiguredOriginAlreadyMatchesAlias()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        var reachedNext = false;
        var middleware = new PublicUrlMiddleware(_ =>
        {
            reachedNext = true;
            return Task.CompletedTask;
        }, environment.Object);
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("www.eatmymovies.com");
        context.Request.Scheme = "https";
        context.Request.Method = "GET";
        context.Request.Path = "/about";
        await middleware.InvokeAsync(context, new SeoMetadataService(Options.Create(new SeoOptions { PublicOrigin = "https://www.eatmymovies.com" })));
        Assert.True(reachedNext);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("https://eatmymovies.com", true)]
    [InlineData("https://eatmymovies.com/", true)]
    [InlineData("http://eatmymovies.com", false)]
    [InlineData("https://eatmymovies.com/path", false)]
    [InlineData("https://eatmymovies.com?query=1", false)]
    [InlineData("https://eatmymovies.com/#fragment", false)]
    [InlineData("https://user:password@eatmymovies.com", false)]
    [InlineData("https://eatmymovies.com:7018", false)]
    [InlineData("", false)]
    public void PublicOrigin_RequiresAnHttpsOrigin(string origin, bool valid)
    {
        var options = new SeoOptions { PublicOrigin = origin };
        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), [], true));
    }

    [Fact]
    public void Metadata_UsesConfiguredOrigin_AndHandlesMissingMovieData()
    {
        var seo = new SeoMetadataService(Options.Create(new SeoOptions { PublicOrigin = "https://movies.example/" }));
        var movie = seo.Build("Movie", "Detail", new MovieDetail { TmdbId = 42, Title = "Undated Film", ReleaseDate = "Unknown" });
        Assert.Equal("Undated Film – Movie Details | Eat My Movies", movie.Title);
        Assert.Equal("https://movies.example/movie/detail?tmdbId=42", movie.CanonicalUrl);
        Assert.Equal("Explore movie details for Undated Film on Eat My Movies.", movie.Description);
        Assert.Equal("https://movies.example/brand/bitten-o-red-my.png", movie.ImageUrl);
        var list = seo.Build("List", "Comedies", new MovieList { Name = "Edited name", CurrentPage = 1 });
        Assert.Equal("https://movies.example/list/comedies", list.CanonicalUrl);
        Assert.Contains("comedy movies", list.Description);
    }
}
