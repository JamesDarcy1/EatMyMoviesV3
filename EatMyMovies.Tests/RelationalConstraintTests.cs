using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.Tests;

public class RelationalConstraintTests
{
    [Fact]
    public async Task UniqueLookupConstraints_RejectDuplicates()
    {
        using var connection = CreateOpenConnection();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        context.Lists.AddRange(
            TestHelpers.CreateList("Top 100"),
            TestHelpers.CreateList("Top 100"));
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.Movies.AddRange(
            TestHelpers.CreateStoreMovie("Alien", 348),
            TestHelpers.CreateStoreMovie("Alien", 349));
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.Movies.AddRange(
            TestHelpers.CreateStoreMovie("Aliens", 679),
            TestHelpers.CreateStoreMovie("Alien 3", 679));
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.Genres.AddRange(
            new Genre { GenreId = Guid.NewGuid(), Name = "Horror" },
            new Genre { GenreId = Guid.NewGuid(), Name = "Horror" });
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task RankingAndGenreConstraints_RejectDuplicatesAndNonPositiveValues()
    {
        using var connection = CreateOpenConnection();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        var list = TestHelpers.CreateList("Top 100");
        var firstMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var secondMovie = TestHelpers.CreateStoreMovie("Aliens", 679);
        var genre = new Genre { GenreId = Guid.NewGuid(), Name = "Horror" };
        context.AddRange(list, firstMovie, secondMovie, genre);
        await context.SaveChangesAsync();

        context.ListRankings.AddRange(
            CreateRanking(list, firstMovie, 1),
            CreateRanking(list, secondMovie, 1));
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.ListRankings.AddRange(
            new ListRanking { ListId = list.ListId, MovieId = firstMovie.MovieId, Ranking = 1 },
            new ListRanking { ListId = list.ListId, MovieId = firstMovie.MovieId, Ranking = 2 });
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.MovieGenres.Add(new MovieGenre { MovieId = firstMovie.MovieId, GenreId = genre.GenreId });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        context.MovieGenres.Add(new MovieGenre { MovieId = firstMovie.MovieId, GenreId = genre.GenreId });
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.ListRankings.Add(new ListRanking { ListId = list.ListId, MovieId = firstMovie.MovieId, Ranking = 0 });
        await AssertDbUpdateExceptionAsync(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddMovieToListAtRankingAsync_ShiftsOccupiedRankingsWithoutConstraintFailures()
    {
        using var connection = CreateOpenConnection();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        var list = TestHelpers.CreateList("Top 100");
        var firstMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var secondMovie = TestHelpers.CreateStoreMovie("Aliens", 679);
        var insertedMovie = TestHelpers.CreateStoreMovie("The Thing", 1091);
        context.AddRange(list, firstMovie, secondMovie, insertedMovie);
        context.ListRankings.AddRange(
            CreateRanking(list, firstMovie, 1),
            CreateRanking(list, secondMovie, 2));
        await context.SaveChangesAsync();
        var repository = new RankingRepository(context);

        await repository.AddMovieToListAtRankingAsync(insertedMovie.MovieId, list.ListId, 2);

        var rankings = await context.ListRankings
            .AsNoTracking()
            .OrderBy(ranking => ranking.Ranking)
            .Select(ranking => new { ranking.MovieId, ranking.Ranking })
            .ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, rankings.Select(ranking => ranking.Ranking));
        Assert.Equal(insertedMovie.MovieId, rankings[1].MovieId);
        Assert.Equal(secondMovie.MovieId, rankings[2].MovieId);
    }

    [Fact]
    public async Task MoveMovieWithinListAsync_MovesRowsWithoutDuplicateMembershipOrRankSlots()
    {
        using var connection = CreateOpenConnection();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        var list = TestHelpers.CreateList("Top 100");
        var firstMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var secondMovie = TestHelpers.CreateStoreMovie("Aliens", 679);
        var thirdMovie = TestHelpers.CreateStoreMovie("The Thing", 1091);
        context.AddRange(list, firstMovie, secondMovie, thirdMovie);
        context.ListRankings.AddRange(
            CreateRanking(list, firstMovie, 1),
            CreateRanking(list, secondMovie, 2),
            CreateRanking(list, thirdMovie, 3));
        await context.SaveChangesAsync();
        var repository = new RankingRepository(context);

        await repository.MoveMovieWithinListAsync(thirdMovie.MovieId, list.ListId, 1);

        var rankings = await context.ListRankings
            .AsNoTracking()
            .OrderBy(ranking => ranking.Ranking)
            .Select(ranking => new { ranking.MovieId, ranking.Ranking })
            .ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, rankings.Select(ranking => ranking.Ranking));
        Assert.Equal(thirdMovie.MovieId, rankings[0].MovieId);
        Assert.Equal(3, rankings.Select(ranking => ranking.MovieId).Distinct().Count());

        await repository.MoveMovieWithinListAsync(thirdMovie.MovieId, list.ListId, 3);

        rankings = await context.ListRankings
            .AsNoTracking()
            .OrderBy(ranking => ranking.Ranking)
            .Select(ranking => new { ranking.MovieId, ranking.Ranking })
            .ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, rankings.Select(ranking => ranking.Ranking));
        Assert.Equal(thirdMovie.MovieId, rankings[2].MovieId);
        Assert.Equal(3, rankings.Select(ranking => ranking.MovieId).Distinct().Count());
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static ListRanking CreateRanking(List list, Movie movie, int ranking)
    {
        return new ListRanking
        {
            ListId = list.ListId,
            List = list,
            MovieId = movie.MovieId,
            Movie = movie,
            Ranking = ranking
        };
    }

    private static async Task AssertDbUpdateExceptionAsync(Func<Task> action)
    {
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }
}
