using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Services;
using Moq;
using DataMovie = EatMyMovies.DataAccess.Models.Movie;

namespace EatMyMovies.Tests;

public class AdminContentServiceTests
{
    [Fact]
    public async Task AddStoredMovieToListAsync_InsertsWhenMovieIsNotAlreadyInList()
    {
        var list = TestHelpers.CreateList("Top 100");
        var movie = TestHelpers.CreateStoreMovie("Alien");
        var listRepository = new Mock<IListRepository>();
        var movieRepository = new Mock<IMovieRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        listRepository.Setup(repository => repository.GetListByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        movieRepository.Setup(repository => repository.GetMovieByIdAsync(movie.MovieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(movie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(listRepository, movieRepository, rankingRepository: rankingRepository);

        await service.AddStoredMovieToListAsync(list.ListId, movie.MovieId, 4);

        rankingRepository.Verify(repository => repository.AddMovieToListAtRankingAsync(
            movie.MovieId,
            list.ListId,
            4,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddStoredMovieToListAsync_ThrowsWhenMovieAlreadyExistsInList()
    {
        var list = TestHelpers.CreateList("Top 100");
        var movie = TestHelpers.CreateStoreMovie("Alien");
        var listRepository = new Mock<IListRepository>();
        var movieRepository = new Mock<IMovieRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        listRepository.Setup(repository => repository.GetListByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        movieRepository.Setup(repository => repository.GetMovieByIdAsync(movie.MovieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(movie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(listRepository, movieRepository, rankingRepository: rankingRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddStoredMovieToListAsync(list.ListId, movie.MovieId, 4));

        Assert.Equal("Movie is already in this list.", exception.Message);
        rankingRepository.Verify(repository => repository.AddMovieToListAtRankingAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddTmdbMovieToListAsync_ReusesExistingStoredMovie()
    {
        var list = TestHelpers.CreateList("Top 100");
        var movie = TestHelpers.CreateStoreMovie("Alien", 348);
        var listRepository = new Mock<IListRepository>();
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var rankingRepository = new Mock<IRankingRepository>();
        listRepository.Setup(repository => repository.GetListByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        movieRepository.Setup(repository => repository.GetMovieByTmdbIdAsync(348, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        movieRepository.Setup(repository => repository.GetMovieByIdAsync(movie.MovieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(movie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(listRepository, movieRepository, movieService, rankingRepository);

        await service.AddTmdbMovieToListAsync(list.ListId, 348, 2);

        movieService.Verify(service => service.GetMovieById(It.IsAny<int>()), Times.Never);
        movieRepository.Verify(repository => repository.SaveTmdbMovieAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<decimal?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
        rankingRepository.Verify(repository => repository.AddMovieToListAtRankingAsync(movie.MovieId, list.ListId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTmdbMovieToListAsync_SavesMissingMovieAndGenresBeforeInsert()
    {
        var list = TestHelpers.CreateList("Top 100");
        var tmdbMovie = TestHelpers.CreateTmdbMovie(id: 348, title: "Alien", genres: new[] { "Horror", "Science Fiction" });
        var savedMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var listRepository = new Mock<IListRepository>();
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var rankingRepository = new Mock<IRankingRepository>();
        listRepository.Setup(repository => repository.GetListByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        movieRepository.Setup(repository => repository.GetMovieByTmdbIdAsync(348, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataMovie)null!);
        movieRepository.Setup(repository => repository.GetMovieByIdAsync(savedMovie.MovieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMovie);
        movieRepository.Setup(repository => repository.SaveTmdbMovieAsync("Alien", 348, 8.5m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMovie);
        movieService.Setup(service => service.GetMovieById(348))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("Alien"))
            .ReturnsAsync(8.5m);
        rankingRepository.Setup(repository => repository.FilmExistsInListAsync(savedMovie.MovieId, list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(listRepository, movieRepository, movieService, rankingRepository);

        await service.AddTmdbMovieToListAsync(list.ListId, 348, 1);

        movieRepository.Verify(repository => repository.SaveGenresAsync(
            savedMovie.MovieId,
            It.Is<IEnumerable<string>>(genres => genres.SequenceEqual(new[] { "Horror", "Science Fiction" })),
            It.IsAny<CancellationToken>()),
            Times.Once);
        rankingRepository.Verify(repository => repository.AddMovieToListAtRankingAsync(savedMovie.MovieId, list.ListId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMovieOfTheWeekAsync_ReusesExistingStoredMovie()
    {
        var movie = TestHelpers.CreateStoreMovie("Alien", 348);
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTmdbIdAsync(348, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);
        var service = CreateService(
            movieRepository: movieRepository,
            movieService: movieService,
            movieOfTheWeekRepository: movieOfTheWeekRepository);

        await service.SetMovieOfTheWeekAsync(348);

        movieService.Verify(service => service.GetMovieById(It.IsAny<int>()), Times.Never);
        movieRepository.Verify(repository => repository.SaveTmdbMovieAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<decimal?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
        movieOfTheWeekRepository.Verify(repository => repository.SetSelectionAsync(movie.MovieId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMovieOfTheWeekAsync_SavesMissingMovieAndGenresBeforeSelection()
    {
        var tmdbMovie = TestHelpers.CreateTmdbMovie(id: 348, title: "Alien", genres: new[] { "Horror", "Science Fiction" });
        var savedMovie = TestHelpers.CreateStoreMovie("Alien", 348);
        var movieRepository = new Mock<IMovieRepository>();
        var movieService = new Mock<IMovieService>();
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        movieRepository.Setup(repository => repository.GetMovieByTmdbIdAsync(348, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataMovie)null!);
        movieRepository.Setup(repository => repository.SaveTmdbMovieAsync("Alien", 348, 8.5m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMovie);
        movieService.Setup(service => service.GetMovieById(348))
            .ReturnsAsync(tmdbMovie);
        movieService.Setup(service => service.GetImdbRating("Alien"))
            .ReturnsAsync(8.5m);
        var service = CreateService(
            movieRepository: movieRepository,
            movieService: movieService,
            movieOfTheWeekRepository: movieOfTheWeekRepository);

        await service.SetMovieOfTheWeekAsync(348);

        movieRepository.Verify(repository => repository.SaveGenresAsync(
            savedMovie.MovieId,
            It.Is<IEnumerable<string>>(genres => genres.SequenceEqual(new[] { "Horror", "Science Fiction" })),
            It.IsAny<CancellationToken>()),
            Times.Once);
        movieOfTheWeekRepository.Verify(repository => repository.SetSelectionAsync(savedMovie.MovieId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearMovieOfTheWeekAsync_DelegatesToRepository()
    {
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        var service = CreateService(movieOfTheWeekRepository: movieOfTheWeekRepository);

        await service.ClearMovieOfTheWeekAsync();

        movieOfTheWeekRepository.Verify(repository => repository.ClearSelectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildDashboardAsync_IncludesCurrentMovieOfTheWeekState()
    {
        var movie = TestHelpers.CreateStoreMovie("Alien", 348);
        var selection = new MovieOfTheWeekSelection
        {
            MovieId = movie.MovieId,
            Movie = movie,
            UpdatedUtc = DateTime.UtcNow
        };
        var listRepository = new Mock<IListRepository>();
        var movieRepository = new Mock<IMovieRepository>();
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        var rankingRepository = new Mock<IRankingRepository>();
        movieRepository.Setup(repository => repository.CountMoviesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        listRepository.Setup(repository => repository.CountListsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        rankingRepository.Setup(repository => repository.CountRankingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        movieOfTheWeekRepository.Setup(repository => repository.GetSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(selection);
        var service = CreateService(
            listRepository,
            movieRepository,
            rankingRepository: rankingRepository,
            movieOfTheWeekRepository: movieOfTheWeekRepository);

        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(4, dashboard.MovieCount);
        Assert.Equal(3, dashboard.ListCount);
        Assert.Equal(2, dashboard.RankingCount);
        Assert.Equal("Alien", dashboard.MovieOfTheWeekTitle);
        Assert.Equal(348, dashboard.MovieOfTheWeekTmdbId);
    }

    [Fact]
    public async Task BuildMovieOfTheWeekAsync_ReturnsCurrentSelectionAndTmdbResults()
    {
        var movie = TestHelpers.CreateStoreMovie("Alien", 348);
        var selection = new MovieOfTheWeekSelection
        {
            MovieId = movie.MovieId,
            Movie = movie,
            UpdatedUtc = DateTime.UtcNow
        };
        var movieService = new Mock<IMovieService>();
        var movieOfTheWeekRepository = new Mock<IMovieOfTheWeekRepository>();
        movieOfTheWeekRepository.Setup(repository => repository.GetSelectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(selection);
        movieService.Setup(service => service.SearchMoviesByTitle("ali"))
            .ReturnsAsync(new List<MovieDropdown>
            {
                new() { Id = 348, Title = "Alien", PosterPath = "/alien.jpg" }
            });
        var service = CreateService(
            movieService: movieService,
            movieOfTheWeekRepository: movieOfTheWeekRepository);

        var model = await service.BuildMovieOfTheWeekAsync("ali");

        Assert.Equal("Alien", model.CurrentTitle);
        Assert.Equal(348, model.CurrentTmdbId);
        Assert.Equal("ali", model.TmdbQuery);
        var result = Assert.Single(model.TmdbSearchResults);
        Assert.Equal("Alien", result.Title);
    }

    [Fact]
    public async Task MoveAndRemoveMovieFromList_DelegateToRankingRepository()
    {
        var listId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var rankingRepository = new Mock<IRankingRepository>();
        var service = CreateService(rankingRepository: rankingRepository);

        await service.MoveMovieWithinListAsync(listId, movieId, 3);
        await service.RemoveMovieFromListAsync(listId, movieId);

        rankingRepository.Verify(repository => repository.MoveMovieWithinListAsync(movieId, listId, 3, It.IsAny<CancellationToken>()), Times.Once);
        rankingRepository.Verify(repository => repository.RemoveListRankingAndCloseGapAsync(movieId, listId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AdminContentService CreateService(
        Mock<IListRepository>? listRepository = null,
        Mock<IMovieRepository>? movieRepository = null,
        Mock<IMovieService>? movieService = null,
        Mock<IRankingRepository>? rankingRepository = null,
        Mock<IMovieOfTheWeekRepository>? movieOfTheWeekRepository = null)
    {
        return new AdminContentService(
            (listRepository ?? new Mock<IListRepository>()).Object,
            (movieRepository ?? new Mock<IMovieRepository>()).Object,
            (movieOfTheWeekRepository ?? new Mock<IMovieOfTheWeekRepository>()).Object,
            (movieService ?? new Mock<IMovieService>()).Object,
            (rankingRepository ?? new Mock<IRankingRepository>()).Object);
    }
}
