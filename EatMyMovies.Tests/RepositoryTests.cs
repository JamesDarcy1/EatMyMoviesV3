using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;

namespace EatMyMovies.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task RankingRepository_GetMoviesForListByPageAsync_ReturnsRankingOrderAndPaginates()
    {
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Top 100");
        context.Lists.Add(list);
        for (var ranking = 1; ranking <= 12; ranking++)
        {
            var movie = TestHelpers.CreateStoreMovie($"Movie {ranking}", ranking);
            context.Movies.Add(movie);
            context.ListRankings.Add(new ListRanking
            {
                ListId = list.ListId,
                List = list,
                MovieId = movie.MovieId,
                Movie = movie,
                Ranking = ranking
            });
        }
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var pageOne = await repository.GetMoviesForListByPageAsync("Top 100", 1);
        var pageTwo = await repository.GetMoviesForListByPageAsync("Top 100", 2);

        Assert.Equal(10, pageOne.Count);
        Assert.Equal("Movie 1", pageOne.First().Title);
        Assert.Equal("Movie 10", pageOne.Last().Title);
        Assert.Equal(new[] { "Movie 11", "Movie 12" }, pageTwo.Select(movie => movie.Title));
        Assert.Equal(new[] { 11, 12 }, pageTwo.Select(movie => movie.Ranking));
    }

    [Fact]
    public async Task RankingRepository_PreservesCurrentRankingOperations()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        var list = TestHelpers.CreateList("Comedies");
        var movie = TestHelpers.CreateStoreMovie("Some Like It Hot");
        context.Lists.Add(list);
        context.Movies.Add(movie);
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var inserted = await repository.AddMovieToListAtRankingAsync(movie.MovieId, list.ListId, 3);

        Assert.True(await repository.FilmExistsInListAsync(movie.MovieId, list.ListId));
        Assert.Equal(3, await repository.GetRankingOfMovieAsync(movie.MovieId, "Comedies"));

        var found = await repository.GetListRankingAsync(movie.MovieId, list.ListId);
        Assert.NotNull(found);
        Assert.Equal(inserted.ListRankingId, found.ListRankingId);

        var updated = await repository.MoveMovieWithinListAsync(inserted.MovieId, inserted.ListId, 1);

        Assert.Equal(1, updated.Ranking);

        await repository.RemoveListRankingAsync(movie.MovieId, list.ListId);

        Assert.False(await repository.FilmExistsInListAsync(movie.MovieId, list.ListId));
    }

    [Fact]
    public async Task ListRepository_AddListAndGetListByNameAsync_PreservesNameAndDescription()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new ListRepository(context);

        var added = await repository.AddListAsync("Documentaries", "Real stories");
        var found = await repository.GetListByNameAsync("Documentaries");

        Assert.NotNull(found);
        Assert.Equal(added.ListId, found.ListId);
        Assert.Equal("Documentaries", found.Name);
        Assert.Equal("Real stories", found.Description);
    }

    [Fact]
    public async Task ListRepository_GetListByNameAsync_ReturnsNullWhenListDoesNotExist()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new ListRepository(context);

        var found = await repository.GetListByNameAsync("Missing");

        Assert.Null(found);
    }

    [Fact]
    public async Task MovieRepository_PreservesMovieAndGenrePersistence()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new MovieRepository(context);

        var movie = await repository.SaveTmdbMovieAsync("Alien", 348, 8.5m);
        await repository.SaveGenresAsync(movie.MovieId, new[] { "Horror", "Science Fiction" });

        var found = await repository.GetMovieByTitleAsync("Alien");
        var horrorMovies = await repository.GetMoviesOfGenresAsync(new List<string> { "Horror" });
        var allGenres = (await repository.GetAllGenresAsync()).Select(genre => genre.Name).ToList();

        Assert.NotNull(found);
        Assert.Equal(movie.MovieId, found.MovieId);
        Assert.Equal(348, found.TmdbId);
        Assert.Contains(horrorMovies, storedMovie => storedMovie.MovieId == movie.MovieId);
        Assert.Contains("Horror", allGenres);
        Assert.Contains("Science Fiction", allGenres);
    }

    [Fact]
    public async Task MovieRepository_SaveGenresAsync_DoesNotDuplicateExistingMovieGenreLinks()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new MovieRepository(context);
        var movie = await repository.SaveTmdbMovieAsync("Alien", 348, 8.5m);

        await repository.SaveGenresAsync(movie.MovieId, new[] { "Horror", "Science Fiction", "Horror" });
        await repository.SaveGenresAsync(movie.MovieId, new[] { "Horror", "Science Fiction" });

        Assert.Equal(2, context.Genres.Count());
        Assert.Equal(2, context.MovieGenres.Count(movieGenre => movieGenre.MovieId == movie.MovieId));
    }

    [Fact]
    public async Task MovieRepository_GetMoviesOfGenresAsync_ReturnsDistinctMovieSummaries()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new MovieRepository(context);
        var movie = await repository.SaveTmdbMovieAsync("Alien", 348, 8.5m);
        await repository.SaveGenresAsync(movie.MovieId, new[] { "Horror", "Science Fiction" });

        var movies = await repository.GetMoviesOfGenresAsync(new[] { "Horror", "Science Fiction" });

        Assert.Single(movies);
        Assert.Equal(movie.MovieId, movies[0].MovieId);
        Assert.Equal("Alien", movies[0].Title);
    }

    [Fact]
    public async Task MovieOfTheWeekRepository_ReturnsNullWhenSelectionDoesNotExist()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new MovieOfTheWeekRepository(context);

        var selection = await repository.GetSelectionAsync();

        Assert.Null(selection);
    }

    [Fact]
    public async Task MovieOfTheWeekRepository_SetsReplacesAndClearsSingletonSelection()
    {
        using var context = TestHelpers.CreateContext();
        var firstMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var secondMovie = TestHelpers.CreateStoreMovie("Heat", 949);
        context.Movies.AddRange(firstMovie, secondMovie);
        context.SaveChanges();
        var repository = new MovieOfTheWeekRepository(context);

        await repository.SetSelectionAsync(firstMovie.MovieId);
        var firstSelection = await repository.GetSelectionAsync();

        Assert.NotNull(firstSelection);
        Assert.Equal(firstMovie.MovieId, firstSelection.MovieId);
        Assert.Equal("Alien", firstSelection.Movie.Title);
        Assert.Single(context.MovieOfTheWeekSelections);

        await repository.SetSelectionAsync(secondMovie.MovieId);
        var secondSelection = await repository.GetSelectionAsync();

        Assert.NotNull(secondSelection);
        Assert.Equal(secondMovie.MovieId, secondSelection.MovieId);
        Assert.Equal("Heat", secondSelection.Movie.Title);
        Assert.Single(context.MovieOfTheWeekSelections);

        await repository.ClearSelectionAsync();

        Assert.Null(await repository.GetSelectionAsync());
        Assert.Empty(context.MovieOfTheWeekSelections);
    }

    [Fact]
    public async Task MovieRepository_SearchMoviesAsync_ReturnsPagedAdminSummariesWithListCounts()
    {
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Top 100");
        var alien = TestHelpers.CreateStoreMovie("Alien", 348);
        var aliens = TestHelpers.CreateStoreMovie("Aliens", 679);
        var heat = TestHelpers.CreateStoreMovie("Heat", 949);
        context.Lists.Add(list);
        context.Movies.AddRange(alien, aliens, heat);
        context.ListRankings.Add(new ListRanking
        {
            ListId = list.ListId,
            MovieId = alien.MovieId,
            Ranking = 1
        });
        context.SaveChanges();
        var repository = new MovieRepository(context);

        var count = await repository.CountMoviesAsync("Alien");
        var results = await repository.SearchMoviesAsync("Alien", page: 1, pageSize: 1);

        Assert.Equal(2, count);
        var result = Assert.Single(results);
        Assert.Equal("Alien", result.Title);
        Assert.Equal(1, result.ListCount);
    }

    [Fact]
    public async Task ListRepository_GetListSummariesAndUpdateListAsync_ReturnsAdminListState()
    {
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Comedies", "Funny things");
        var movie = TestHelpers.CreateStoreMovie("Some Like It Hot");
        context.Lists.Add(list);
        context.Movies.Add(movie);
        context.ListRankings.Add(new ListRanking
        {
            ListId = list.ListId,
            MovieId = movie.MovieId,
            Ranking = 1
        });
        context.SaveChanges();
        var repository = new ListRepository(context);

        await repository.UpdateListAsync(list.ListId, "Great Comedies", "Still funny");
        var summaries = await repository.GetListSummariesAsync();
        var foundById = await repository.GetListByIdAsync(list.ListId);

        var summary = Assert.Single(summaries);
        Assert.Equal("Great Comedies", summary.Name);
        Assert.Equal("Still funny", summary.Description);
        Assert.Equal(1, summary.MovieCount);
        Assert.NotNull(foundById);
        Assert.Equal("Great Comedies", foundById.Name);
    }

    [Fact]
    public async Task RankingRepository_AdminRows_ReturnListRowsAndMovieMemberships()
    {
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Horrors");
        var movie = TestHelpers.CreateStoreMovie("Alien", 348);
        context.Lists.Add(list);
        context.Movies.Add(movie);
        context.ListRankings.Add(new ListRanking
        {
            ListId = list.ListId,
            MovieId = movie.MovieId,
            Ranking = 2
        });
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var listRows = await repository.GetListMovieRowsAsync(list.ListId);
        var memberships = await repository.GetMovieMembershipsAsync(movie.MovieId);

        var listRow = Assert.Single(listRows);
        Assert.Equal("Alien", listRow.Title);
        Assert.Equal(348, listRow.TmdbId);
        Assert.Equal(2, listRow.Ranking);
        var membership = Assert.Single(memberships);
        Assert.Equal("Horrors", membership.ListName);
        Assert.Equal(2, membership.Ranking);
    }

    [Fact]
    public async Task RankingRepository_RemoveListRankingAndCloseGapAsync_RemovesMembershipAndCompactsRanks()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = TestHelpers.CreateSqliteContext(connection);
        var list = TestHelpers.CreateList("Top 100");
        var firstMovie = TestHelpers.CreateStoreMovie("Movie 1", 1);
        var secondMovie = TestHelpers.CreateStoreMovie("Movie 2", 2);
        var thirdMovie = TestHelpers.CreateStoreMovie("Movie 3", 3);
        context.Lists.Add(list);
        context.Movies.AddRange(firstMovie, secondMovie, thirdMovie);
        context.ListRankings.AddRange(
            new ListRanking { ListId = list.ListId, MovieId = firstMovie.MovieId, Ranking = 1 },
            new ListRanking { ListId = list.ListId, MovieId = secondMovie.MovieId, Ranking = 2 },
            new ListRanking { ListId = list.ListId, MovieId = thirdMovie.MovieId, Ranking = 3 });
        context.SaveChanges();
        var repository = new RankingRepository(context);

        await repository.RemoveListRankingAndCloseGapAsync(secondMovie.MovieId, list.ListId);

        var rankings = context.ListRankings
            .OrderBy(ranking => ranking.Ranking)
            .Select(ranking => new { ranking.MovieId, ranking.Ranking })
            .ToList();
        Assert.Equal(new[] { firstMovie.MovieId, thirdMovie.MovieId }, rankings.Select(ranking => ranking.MovieId));
        Assert.Equal(new[] { 1, 2 }, rankings.Select(ranking => ranking.Ranking));
    }
}
