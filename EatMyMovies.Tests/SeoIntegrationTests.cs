using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using EatMyMoviesSite;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Models.Admin;
using EatMyMoviesSite.Services;
using EatMyMovies.DataAccess.Models;
using EatMyMovies.DataAccess.Repositories;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EatMyMovies.Tests;

public class SeoIntegrationTests : IClassFixture<SeoApplication>
{
    private readonly SeoApplication _app;
    private readonly HttpClient _client;

    public SeoIntegrationTests(SeoApplication app)
    {
        _app = app;
        _client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/", "Eat My Movies | Find Your Next Movie")]
    [InlineData("/about", "About | Eat My Movies")]
    [InlineData("/contact", "Contact | Eat My Movies")]
    [InlineData("/privacy", "Privacy | Eat My Movies")]
    [InlineData("/list", "Movie Lists | Eat My Movies")]
    [InlineData("/movie/recommender", "What Should I Watch? Movie Quiz | Eat My Movies")]
    [InlineData("/movie/spin-the-wheel", "Movie Picker Wheel | Eat My Movies")]
    [InlineData("/list/top-100", "Top 100 Movies | Eat My Movies")]
    [InlineData("/list/comedies", "Comedy Movies | Eat My Movies")]
    [InlineData("/list/foreign-films", "Foreign Language Films | Eat My Movies")]
    [InlineData("/list/documentaries", "Documentary Movies | Eat My Movies")]
    [InlineData("/list/christmas", "Christmas Movies | Eat My Movies")]
    [InlineData("/list/standout-soundtracks", "Movies with Standout Soundtracks | Eat My Movies")]
    [InlineData("/list/iconic-80s", "Iconic 80s Movies | Eat My Movies")]
    [InlineData("/list/disney", "Disney Movies | Eat My Movies")]
    [InlineData("/list/horrors", "Horror Movies | Eat My Movies")]
    [InlineData("/list/top-100?page=2", "Top 100 Movies – Page 2 | Eat My Movies")]
    [InlineData("/movie/detail?tmdbId=42", "Alien (1979) – Movie Details | Eat My Movies")]
    public async Task PublicPages_RenderUniqueMetadataInHead(string path, string title)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var head = Head(html);
        Assert.Equal(title, WebUtility.HtmlDecode(Regex.Match(head, "<title>(.*?)</title>").Groups[1].Value));
        Assert.Single(Regex.Matches(html, "<title>"));
        Assert.Single(Regex.Matches(html, "name=\"description\""));
        Assert.Single(Regex.Matches(html, "rel=\"canonical\""));
        Assert.Contains($"href=\"https://eatmymovies.com{path}\"", head);
        Assert.Contains("property=\"og:title\"", head);
        Assert.Contains("property=\"og:image:alt\"", head);
        Assert.DoesNotContain("<title>", html[(html.IndexOf("</head>") + 7)..]);
    }

    [Fact]
    public async Task HomeStructuredData_AndSharingLinks_UseConfiguredOrigin()
    {
        var html = await _client.GetStringAsync("/?utm_source=test");
        var json = Regex.Match(Head(html), "<script type=\"application/ld(?:\\+|&#x2B;)json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
        Assert.True(json.Length > 0, Head(html));
        using var document = JsonDocument.Parse(json);
        Assert.Equal("WebSite", document.RootElement.GetProperty("@type").GetString());
        Assert.Equal("Eat My Movies", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("https://eatmymovies.com/", document.RootElement.GetProperty("url").GetString());
        Assert.DoesNotContain("utm_source", Head(html));
        var recommender = await _client.GetStringAsync("/movie/recommender");
        Assert.Contains("https://eatmymovies.com/movie/detail?tmdbId=", recommender);
        Assert.DoesNotContain("www.eatmymovies.com", recommender);
    }

    [Fact]
    public async Task Sitemap_ContainsOnlySelectedCanonicalPages()
    {
        var response = await _client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var locations = xml.Descendants().Where(element => element.Name.LocalName == "loc")
            .Select(element => element.Value).ToArray();
        Assert.Contains("https://eatmymovies.com/list", locations);
        Assert.Contains("https://eatmymovies.com/list/top-100?page=3", locations);
        Assert.Contains("https://eatmymovies.com/movie/detail?tmdbId=42", locations);
        Assert.Contains("https://eatmymovies.com/movie/detail?tmdbId=99", locations);
        Assert.DoesNotContain(locations, url => url.Contains("/admin") || url.Contains("/movie/search"));
        Assert.Equal(locations.Length, locations.Distinct().Count());
        var robots = await _client.GetStringAsync("/robots.txt");
        Assert.Contains("Sitemap: https://eatmymovies.com/sitemap.xml", robots);
        Assert.DoesNotContain("Disallow: /admin", robots);
    }

    [Fact]
    public async Task SearchAndJsonResponses_AreNoIndex_AndBreadcrumbsAreVisible()
    {
        var search = Head(await _client.GetStringAsync("/movie/search"));
        Assert.Contains("name=\"robots\" content=\"noindex\"", search);
        Assert.DoesNotContain("rel=\"canonical\"", search);
        var json = await _client.GetAsync("/movie/SearchForMovie?titleSearch=Alien");
        Assert.Equal("noindex", json.Headers.GetValues("X-Robots-Tag").Single());
        foreach (var path in new[] { "/list", "/list/top-100", "/movie/detail?tmdbId=42" })
            Assert.Contains("aria-label=\"Breadcrumb\"", await _client.GetStringAsync(path));
        var recommender = await _client.GetStringAsync("/movie/recommender");
        Assert.True(recommender.IndexOf("Find a movie for tonight", StringComparison.Ordinal) < recommender.IndexOf("id=\"recommenderApp\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TrackingRequiresChoice_AndStaticCachingDependsOnVersioning()
    {
        var home = await _client.GetStringAsync("/");
        Assert.Contains("id=\"tracking-accept\"", home);
        Assert.Contains("id=\"tracking-reject\"", home);
        Assert.DoesNotContain("src=\"https://www.googletagmanager.com", home);
        Assert.DoesNotContain("src=\"https://static.hotjar.com", home);
        var versioned = await _client.GetAsync("/css/site.css?v=test");
        var unversioned = await _client.GetAsync("/favicon/site.webmanifest");
        Assert.Contains("max-age=31536000", versioned.Headers.CacheControl?.ToString());
        Assert.Contains("max-age=86400", unversioned.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("/Home", "/")]
    [InlineData("/Home/Index?utm_source=test", "/?utm_source=test")]
    [InlineData("/ABOUT/", "/about")]
    [InlineData("/Movie/Detail?tmdbId=42", "/movie/detail?tmdbId=42")]
    [InlineData("/movie/detail?tmdbId=42&title=Alien", "/movie/detail?tmdbId=42")]
    [InlineData("/list/top-100?page=1", "/list/top-100")]
    [InlineData("/list/top100", "/list/top-100")]
    [InlineData("/list/top100?page=2", "/list/top-100?page=2")]
    public async Task OldUrls_RedirectPermanently_AndTargetDoesNotLoop(string path, string destination)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(destination, response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(destination)).StatusCode);
    }

    [Fact]
    public async Task TitleOnlyLookup_RedirectsTemporarily()
    {
        var response = await _client.GetAsync("/movie/detail?title=Alien");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/movie/detail?tmdbId=42", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/does-not-exist")]
    [InlineData("/missing.css")]
    [InlineData("/home/index/anything")]
    [InlineData("/movie")]
    [InlineData("/movie/detail")]
    [InlineData("/movie/detail?tmdbId=")]
    [InlineData("/movie/detail?tmdbId=abc&title=Alien")]
    [InlineData("/movie/detail?tmdbId=0&title=Alien")]
    [InlineData("/movie/detail?tmdbId=-1")]
    [InlineData("/movie/detail?tmdbId=404")]
    [InlineData("/movie/detail?title=missing")]
    [InlineData("/list/top-100?page=abc")]
    [InlineData("/list/top-100?page=")]
    [InlineData("/list/top-100?page=0")]
    [InlineData("/list/top-100?page=-1")]
    [InlineData("/list/top-100?page=99999")]
    [InlineData("/list/top100?page=abc")]
    public async Task MissingPages_ReturnBranded404WithoutCanonical(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Page not found", html);
        Assert.Contains("Search movies", html);
        Assert.Contains("name=\"robots\" content=\"noindex\"", Head(html));
        Assert.DoesNotContain("rel=\"canonical\"", html);
        Assert.DoesNotContain("property=\"og:", html);
        Assert.Single(Regex.Matches(html, "<title>"));
    }

    [Theory]
    [InlineData(503, "Temporarily unavailable")]
    [InlineData(500, "Something went wrong")]
    public async Task Failures_ReturnAccurateStatus_WithoutIndexingDirectives(int status, string heading)
    {
        var response = await _client.GetAsync($"/movie/detail?tmdbId={status}");
        Assert.Equal(status, (int)response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(heading, html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
        Assert.DoesNotContain("noindex", Head(html));
        Assert.DoesNotContain("Development Mode", html);
    }

    [Fact]
    public async Task Metadata_EncodesUntrustedMovieText_AndOmitsUnknownYear()
    {
        var html = await _client.GetStringAsync("/movie/detail?tmdbId=43&utm_source=test");
        var head = Head(html);
        Assert.Contains("&lt;/title&gt;&lt;script&gt;", head);
        Assert.DoesNotContain("<script>bad", head);
        Assert.DoesNotContain("(Unknown)", head);
        Assert.DoesNotContain("utm_source", head);
        Assert.Contains("https://eatmymovies.com/brand/bitten-o-red-my.png", head);
        var movie = Head(await _client.GetStringAsync("/movie/detail?tmdbId=42"));
        Assert.Contains("https://image.tmdb.org/t/p/w500/alien.jpg", movie);
    }

    [Fact]
    public async Task Pagination_UsesRouteIdentity_NotEditedListName()
    {
        var html = await _client.GetStringAsync("/list/top-100?page=2");
        Assert.Contains("An edited list name", html);
        Assert.Contains("href=\"/list/top-100\"", html);
        Assert.Contains("href=\"/list/top-100?page=3\"", html);
        Assert.DoesNotContain("href=\"/list/An", html);
    }

    [Fact]
    public async Task ProductionAlias_RedirectsWith308_WithoutTrustingForwardedHost()
    {
        using var alias = new HttpRequestMessage(HttpMethod.Post, "https://www.eatmymovies.com/admin/login?returnUrl=%2Fadmin");
        var response = await _client.SendAsync(alias);
        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal("https://eatmymovies.com/admin/login?returnUrl=%2Fadmin", response.Headers.Location?.OriginalString);
        using var spoof = new HttpRequestMessage(HttpMethod.Get, "https://staging.example/about");
        spoof.Headers.Add("X-Forwarded-Host", "www.eatmymovies.com");
        spoof.Headers.Add("X-Forwarded-Proto", "http");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(spoof)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("https://eatmymovies.com/about")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("https://localhost/about")).StatusCode);
        var http = await _client.GetAsync("http://www.eatmymovies.com/about");
        Assert.Equal(HttpStatusCode.PermanentRedirect, http.StatusCode);
        Assert.Equal("https://eatmymovies.com/about", http.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AdminAuthentication_AntiForgery_AndJsonEndpoints_RemainFunctional()
    {
        var login = await _client.GetStringAsync("/admin/login");
        Assert.Contains("name=\"robots\" content=\"noindex\"", Head(login));
        Assert.DoesNotContain("canonical", Head(login));
        Assert.Equal(HttpStatusCode.Found, (await _client.GetAsync("/admin")).StatusCode);
        var missingToken = await _client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "seo-test", ["Password"] = "test-password"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        var token = Regex.Match(login, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var signedIn = await _client.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "seo-test", ["Password"] = "test-password", ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token)
        }));
        Assert.Equal(HttpStatusCode.Found, signedIn.StatusCode);
        Assert.Equal("/admin", signedIn.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/admin/logout", new StringContent(""))).StatusCode);
        var json = await _client.GetAsync("/movie/SearchForMovie?titleSearch=Alien");
        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
        Assert.Equal("application/json", json.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Alien", await json.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/movie/GetFeelings")).StatusCode);
    }

    private static string Head(string html) => Regex.Match(html, "<head>(.*?)</head>", RegexOptions.Singleline).Groups[1].Value;
}

public sealed class SeoApplication : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DbConnection"] = "Server=unused;Database=seo-tests;Trusted_Connection=True;",
            ["Tmdb:ApiKey"] = "test-tmdb-key", ["Omdb:ApiKey"] = "test-omdb-key",
            ["AdminAuth:Username"] = "seo-test", ["AdminAuth:PasswordHash"] = AdminPasswordHasher.HashPassword("test-password"),
            ["Seo:PublicOrigin"] = "https://eatmymovies.com"
        }));
        builder.ConfigureServices(services =>
        {
            var movies = new Mock<IMovieService>();
            movies.Setup(x => x.BuildMovieOfTheWeekAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ListMovie?)null);
            movies.Setup(x => x.BuildMovieList(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((string name, int page, CancellationToken _) => page > 3
                    ? Task.FromException<MovieList>(new InvalidListPageException())
                    : Task.FromResult(new MovieList { Name = "An edited list name", Description = "A curated list.", CurrentPage = page, TotalPages = 3, Movies = [] }));
            movies.Setup(x => x.BuildMovieDetail(It.IsAny<string?>(), It.IsAny<int?>(), false, It.IsAny<CancellationToken>()))
                .Returns((string? _, int? id, bool _, CancellationToken _) => id switch
                {
                    404 => Task.FromException<MovieDetail>(new MovieNotFoundException("Missing")),
                    503 => Task.FromException<MovieDetail>(new MovieDataUnavailableException("Unavailable")),
                    500 => Task.FromException<MovieDetail>(new InvalidOperationException("Unexpected test failure")),
                    _ => Task.FromResult(new MovieDetail
                    {
                        TmdbId = id ?? 42, Title = id == 43 ? "</title><script>bad</script>\" & Movie" : "Alien",
                        ReleaseDate = id == 43 ? "Unknown" : "1979", Overview = "A crew discovers something unexpected. \"Watch\" & enjoy.",
                        PosterPath = id == 43 ? "" : "/alien.jpg", Genres = "Science Fiction", Runtime = 117,
                        Director = new Person { Name = "Director", Biography = "Director biography" }, Actors = [], WatchAvailability = new()
                    })
                });
            movies.Setup(x => x.GetMovieByTitle(It.IsAny<string>())).Returns((string title) => title == "missing"
                ? Task.FromException<TMDbLib.Objects.Movies.Movie>(new MovieNotFoundException("Missing"))
                : Task.FromResult(new TMDbLib.Objects.Movies.Movie { Id = 42, Title = "Alien" }));
            movies.Setup(x => x.SearchMoviesByTitle(It.IsAny<string>())).ReturnsAsync([new MovieDropdown { Id = 42, Title = "Alien", ReleaseYear = 1979 }]);
            services.RemoveAll<IMovieService>();
            services.AddSingleton(movies.Object);
            var admin = new Mock<IAdminContentService>();
            admin.Setup(x => x.BuildDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AdminDashboardViewModel());
            services.RemoveAll<IAdminContentService>();
            services.AddSingleton(admin.Object);
            var lists = new Mock<IListRepository>();
            lists.Setup(repository => repository.GetAllListsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new List { Name = "Top 100" }, new List { Name = "Comedies" }]);
            services.RemoveAll<IListRepository>();
            services.AddSingleton(lists.Object);
            var rankings = new Mock<IRankingRepository>();
            rankings.Setup(repository => repository.GetListCountAsync("Top 100", It.IsAny<CancellationToken>())).ReturnsAsync(21);
            rankings.Setup(repository => repository.GetListCountAsync("Comedies", It.IsAny<CancellationToken>())).ReturnsAsync(1);
            rankings.Setup(repository => repository.GetCuratedTmdbIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([42]);
            services.RemoveAll<IRankingRepository>();
            services.AddSingleton(rankings.Object);
            var weekly = new Mock<IMovieOfTheWeekRepository>();
            weekly.Setup(repository => repository.GetSelectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MovieOfTheWeekSelection { Movie = new Movie { TmdbId = 99 } });
            services.RemoveAll<IMovieOfTheWeekRepository>();
            services.AddSingleton(weekly.Object);
        });
    }
}
