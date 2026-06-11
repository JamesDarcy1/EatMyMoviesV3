using EatMyMoviesSite;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Enums;
using TMDbLib.Objects.General;

namespace EatMyMovies.Tests;

public class MapperTests
{
    [Fact]
    public void BuildListMovie_MapsDisplayFields()
    {
        var movie = TestHelpers.CreateTmdbMovie(
            id: 123,
            title: "Arrival",
            posterPath: "/arrival.jpg",
            originalLanguage: "en",
            runtime: 116,
            releaseYear: 2016,
            "Drama",
            "Science Fiction");

        var result = Mapper.BuildListMovie(movie, 7.9m, 4);

        Assert.Equal("Arrival", result.Title);
        Assert.Equal("https://image.tmdb.org/t/p/w185/arrival.jpg", result.PosterPath);
        Assert.Equal("Drama, Science Fiction", result.Genres);
        Assert.Equal(7.9m, result.ImdbRating);
        Assert.Equal(4, result.Ranking);
        Assert.Equal("A movie used by tests.", result.Synopsis);
        Assert.Equal(116, result.Runtime);
        Assert.Equal("2016", result.ReleaseDate);
        Assert.Equal("English", result.Language);
        Assert.Equal(123, result.TmdbId);
    }

    [Fact]
    public void MapToMovieDetail_MapsTrailerDirectorAndFirstSixActors()
    {
        var movie = TestHelpers.CreateTmdbMovie(
            id: 321,
            title: "The Detail Movie",
            posterPath: "/detail.jpg",
            originalLanguage: "fr",
            runtime: 93,
            releaseYear: 2001,
            "Comedy");
        var trailer = new Video { Key = "abc123" };
        var director = new Person { Id = 1, Name = "A Director", Role = "Director" };
        var actors = Enumerable.Range(1, 8)
            .Select(index => new Person { Id = index, Name = $"Actor {index}", Role = "Actor" })
            .ToList();

        var result = Mapper.MapToMovieDetail(movie, trailer, 8.1m, director, actors);

        Assert.Equal("The Detail Movie", result.Title);
        Assert.Equal("/detail.jpg", result.PosterPath);
        Assert.Equal("/backdrop.jpg", result.BackdropPath);
        Assert.Equal("https://www.youtube.com/embed/abc123", result.TrailerPath);
        Assert.Equal("A useful tagline", result.Tagline);
        Assert.Equal("Comedy", result.Genres);
        Assert.Equal(8.1m, result.ImdbRating);
        Assert.Equal(93, result.Runtime);
        Assert.Equal("2001", result.ReleaseDate);
        Assert.Equal("French", result.Language);
        Assert.Same(director, result.Director);
        Assert.Equal(6, result.Actors.Count);
        Assert.Equal("Actor 6", result.Actors.Last().Name);
    }

    [Fact]
    public void MapToMovieDetail_UsesUnknownWhenReleaseDateIsMissing()
    {
        var movie = TestHelpers.CreateTmdbMovie(releaseYear: null);
        var director = new Person { Id = 1, Name = "A Director", Role = "Director" };

        var result = Mapper.MapToMovieDetail(movie, trailer: null, imdbRating: null, director, new List<Person>());

        Assert.Equal("Unknown", result.ReleaseDate);
        Assert.Null(result.TrailerPath);
    }

    [Fact]
    public void GetFeelingToGenreMapping_PreservesRecommendationMappings()
    {
        var mapping = Mapper.GetFeelingToGenreMapping();

        Assert.Contains("Comedy", mapping[Feeling.FeelGood]);
        Assert.Contains("Family", mapping[Feeling.Funny]);
        Assert.Contains("Horror", mapping[Feeling.Scary]);
        Assert.Contains("Documentary", mapping[Feeling.Inspiring]);
    }
}
