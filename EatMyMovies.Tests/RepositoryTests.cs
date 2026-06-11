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
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Comedies");
        var movie = TestHelpers.CreateStoreMovie("Some Like It Hot");
        context.Lists.Add(list);
        context.Movies.Add(movie);
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var inserted = await repository.InsertMovieToListAsync(movie.MovieId, list.ListId, 3);

        Assert.True(await repository.FilmExistsInListAsync(movie.MovieId, list.ListId));
        Assert.Equal(3, await repository.GetRankingOfMovieAsync(movie.MovieId, "Comedies"));

        var found = await repository.GetListRankingAsync(movie.MovieId, list.ListId);
        Assert.NotNull(found);
        Assert.Equal(inserted.ListRankingId, found.ListRankingId);

        var updated = await repository.UpdateRankingAsync(inserted, 1);

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
}
