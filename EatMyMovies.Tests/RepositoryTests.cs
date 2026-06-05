using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;

namespace EatMyMovies.Tests;

public class RepositoryTests
{
    [Fact]
    public void RankingRepository_GetMoviesForListByPage_ReturnsRankingOrderAndPaginates()
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
                List = list,
                Movie = movie,
                Ranking = ranking
            });
        }
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var pageOne = repository.GetMoviesForListByPage("Top 100", 1).ToList();
        var pageTwo = repository.GetMoviesForListByPage("Top 100", 2).ToList();

        Assert.Equal(10, pageOne.Count);
        Assert.Equal("Movie 1", pageOne.First().Title);
        Assert.Equal("Movie 10", pageOne.Last().Title);
        Assert.Equal(new[] { "Movie 11", "Movie 12" }, pageTwo.Select(movie => movie.Title));
    }

    [Fact]
    public void RankingRepository_PreservesCurrentRankingOperations()
    {
        using var context = TestHelpers.CreateContext();
        var list = TestHelpers.CreateList("Comedies");
        var movie = TestHelpers.CreateStoreMovie("Some Like It Hot");
        context.Lists.Add(list);
        context.Movies.Add(movie);
        context.SaveChanges();
        var repository = new RankingRepository(context);

        var inserted = repository.InsertMovieToList(movie, list, 3);

        Assert.True(repository.FilmExistsInList(movie.MovieId, list.ListId));
        Assert.Equal(3, repository.GetRankingOfMovie(movie.MovieId, "Comedies"));
        Assert.Same(inserted, repository.GetListRanking(movie.MovieId, list.ListId));

        var updated = repository.UpdateRanking(inserted, 1);

        Assert.Equal(1, updated.Ranking);

        repository.RemoveListRanking(movie.MovieId, list.ListId);

        Assert.False(repository.FilmExistsInList(movie.MovieId, list.ListId));
    }

    [Fact]
    public void ListRepository_AddListAndGetListByName_PreservesNameAndDescription()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new ListRepository(context);

        var added = repository.AddList("Documentaries", "Real stories");
        var found = repository.GetListByName("Documentaries");

        Assert.Equal(added.ListId, found.ListId);
        Assert.Equal("Documentaries", found.Name);
        Assert.Equal("Real stories", found.Description);
    }

    [Fact]
    public void MovieRepository_PreservesMovieAndGenrePersistence()
    {
        using var context = TestHelpers.CreateContext();
        var repository = new MovieRepository(context);

        var movie = repository.SaveTmdbMovie("Alien", 348, 8.5m);
        repository.SaveGenres(movie, new[] { "Horror", "Science Fiction" });

        var found = repository.GetMovieByTitle("Alien");
        var horrorMovies = repository.GetMoviesOfGenres(new List<string> { "Horror" }).ToList();
        var allGenres = repository.GetAllGenres().Select(genre => genre.Name).ToList();

        Assert.Equal(movie.MovieId, found.MovieId);
        Assert.Equal(348, found.TmdbId);
        Assert.Contains(horrorMovies, storedMovie => storedMovie.MovieId == movie.MovieId);
        Assert.Contains("Horror", allGenres);
        Assert.Contains("Science Fiction", allGenres);
    }
}
