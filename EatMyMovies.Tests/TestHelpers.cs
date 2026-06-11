using DataList = EatMyMovies.DataAccess.Models.List;
using DataMovie = EatMyMovies.DataAccess.Models.Movie;
using EatMyMovies.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;

namespace EatMyMovies.Tests;

internal static class TestHelpers
{
    public static EatMyMoviesContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EatMyMoviesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EatMyMoviesContext(options);
    }

    public static EatMyMoviesContext CreateSqliteContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<EatMyMoviesContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EatMyMoviesContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Movie CreateTmdbMovie(
        int id = 42,
        string title = "The Test Movie",
        string posterPath = "/poster.jpg",
        string originalLanguage = "en",
        int runtime = 101,
        int? releaseYear = 1999,
        params string[] genres)
    {
        return new Movie
        {
            Id = id,
            Title = title,
            PosterPath = posterPath,
            BackdropPath = "/backdrop.jpg",
            Overview = "A movie used by tests.",
            Runtime = runtime,
            ReleaseDate = releaseYear.HasValue ? new DateTime(releaseYear.Value, 7, 16) : null,
            OriginalLanguage = originalLanguage,
            Tagline = "A useful tagline",
            Genres = genres.DefaultIfEmpty("Drama")
                .Select(name => new Genre { Name = name })
                .ToList()
        };
    }

    public static DataList CreateList(string name = "Top 100", string description = "The best films")
    {
        return new DataList
        {
            ListId = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }

    public static DataMovie CreateStoreMovie(string title = "The Test Movie", int tmdbId = 42)
    {
        return new DataMovie
        {
            MovieId = Guid.NewGuid(),
            Title = title,
            TmdbId = tmdbId
        };
    }
}
