using System.Xml.Linq;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EatMyMoviesSite.Controllers;

public sealed class SeoController(
    SeoMetadataService seo,
    IListRepository lists,
    IRankingRepository rankings,
    IMovieOfTheWeekRepository movieOfTheWeek) : Controller
{
    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content(
        $"User-agent: *\nAllow: /\nSitemap: {seo.AbsoluteUrl("/sitemap.xml")}\n",
        "text/plain; charset=utf-8");

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal)
        {
            "/", "/about", "/contact", "/privacy", "/list",
            "/movie/recommender", "/movie/spin-the-wheel"
        };

        var publishedNames = (await lists.GetAllListsAsync(cancellationToken))
            .Select(list => list.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var category in SeoMetadataService.Lists.Values)
        {
            if (!publishedNames.Contains(category.ListName)) continue;
            var count = await rankings.GetListCountAsync(category.ListName, cancellationToken);
            paths.Add(category.Path);
            var pages = Math.Max(1, (int)Math.Ceiling((double)count / MovieService.ListPageSize));
            for (var page = 2; page <= pages; page++)
                paths.Add($"{category.Path}?page={page}");
        }

        var ids = (await rankings.GetCuratedTmdbIdsAsync(cancellationToken)).ToHashSet();
        var selection = await movieOfTheWeek.GetSelectionAsync(cancellationToken);
        if (selection?.Movie.TmdbId is int selectedId && selectedId > 0)
            ids.Add(selectedId);
        foreach (var id in ids.Where(id => id > 0))
            paths.Add(SeoMetadataService.MoviePath(id));

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(
            new XElement(ns + "urlset", paths.OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new XElement(ns + "url", new XElement(ns + "loc", seo.AbsoluteUrl(path))))));
        return Content(document.ToString(), "application/xml; charset=utf-8");
    }
}
