using System.Globalization;
using System.Text.RegularExpressions;
using EatMyMoviesSite.DTOs;
using EatMyMoviesSite.Models;
using EatMyMoviesSite.Options;
using Microsoft.Extensions.Options;

namespace EatMyMoviesSite.Services;

public sealed class SeoMetadataService(IOptions<SeoOptions> options)
{
    public string PublicOrigin => options.Value.PublicOrigin.TrimEnd('/');
    public string AbsoluteUrl(string path) => PublicOrigin + "/" + path.TrimStart('/');
    public static string MoviePath(int id) => $"/movie/detail?tmdbId={id.ToString(CultureInfo.InvariantCulture)}";

    // Route/action identity is stable even when an administrator edits a list's display name.
    internal static readonly IReadOnlyDictionary<string, (string Path, string Title, string ListName)> Lists =
        new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Top100"] = ("/list/top-100", "Top 100 Movies", "Top 100"),
            ["Comedies"] = ("/list/comedies", "Comedy Movies", "Comedies"),
            ["ForeignFilms"] = ("/list/foreign-films", "Foreign Language Films", "Foreign Films"),
            ["Documentaries"] = ("/list/documentaries", "Documentary Movies", "Documentaries"),
            ["Christmas"] = ("/list/christmas", "Christmas Movies", "Christmas"),
            ["StandoutSoundtracks"] = ("/list/standout-soundtracks", "Movies with Standout Soundtracks", "Standout Soundtracks"),
            ["Iconic80s"] = ("/list/iconic-80s", "Iconic 80s Movies", "Iconic 80s"),
            ["Disney"] = ("/list/disney", "Disney Movies", "Disney"),
            ["Horrors"] = ("/list/horrors", "Horror Movies", "Horrors")
        };

    public PageMetadata Build(string controller, string action, object? model, string? existingTitle = null)
    {
        if (model is ErrorViewModel error)
            return new PageMetadata($"{error.Heading} | Eat My Movies", NoIndex: error.StatusCode == 404);
        if (controller == "Admin")
            return new PageMetadata(existingTitle ?? "Admin | Eat My Movies", NoIndex: true);

        if (controller == "List" && model is MovieList list && Lists.TryGetValue(action, out var category))
        {
            var suffix = list.CurrentPage > 1 ? $" – Page {list.CurrentPage}" : "";
            var path = category.Path + (list.CurrentPage > 1 ? $"?page={list.CurrentPage}" : "");
            var description = string.IsNullOrWhiteSpace(list.Description)
                ? $"Explore our ranked selection of {category.Title.ToLowerInvariant()} and find your next watch."
                : Compact(list.Description, 150);
            if (list.CurrentPage > 1) description = $"Page {list.CurrentPage}. {description}";
            return Public($"{category.Title}{suffix} | Eat My Movies", description, path);
        }
        if (controller == "Movie" && model is MovieDetail movie)
        {
            var year = int.TryParse(movie.ReleaseDate, out var parsedYear) && parsedYear > 0
                ? $" ({parsedYear})" : "";
            var identity = movie.Title + year;
            var description = string.IsNullOrWhiteSpace(movie.Overview)
                ? $"Explore movie details for {identity} on Eat My Movies."
                : $"{identity}: {Compact(movie.Overview, 140)}";
            var metadata = Public($"{identity} – Movie Details | Eat My Movies", description, MoviePath(movie.TmdbId));
            return !string.IsNullOrWhiteSpace(movie.PosterPath) && movie.PosterPath.StartsWith('/') && !movie.PosterPath.StartsWith("//")
                ? metadata with { ImageUrl = "https://image.tmdb.org/t/p/w500" + movie.PosterPath, ImageAlt = movie.Title + " poster" }
                : metadata;
        }

        return (controller, action) switch
        {
            ("Home", "Index") => Public("Eat My Movies | Find Your Next Movie", "Not sure what to watch? Find a movie for your mood, explore curated movie lists, or let the wheel choose your next movie night.", "/") with { IsHome = true },
            ("Home", "About") => Public("About | Eat My Movies", "Meet Eat My Movies: discover movies through recommendations tailored to your taste and carefully curated movie lists.", "/about"),
            ("Home", "Contact") => Public("Contact | Eat My Movies", "Get in touch with Eat My Movies with questions, feedback or enquiries about our movie recommendations and lists.", "/contact"),
            ("Home", "Privacy") => Public("Privacy | Eat My Movies", "How Eat My Movies handles contact messages, essential cookies and optional analytics.", "/privacy"),
            ("List", "Index") => Public("Movie Lists | Eat My Movies", "Browse James's curated movie lists and find a film for your next movie night.", "/list"),
            ("Movie", "Recommender") => Public("What Should I Watch? Movie Quiz | Eat My Movies", "Answer a few questions about your mood, available time and movie preferences to find something to watch tonight.", "/movie/recommender"),
            ("Movie", "SpinTheWheel") => Public("Movie Picker Wheel | Eat My Movies", "Can't choose a movie? Add your movie night contenders and spin the wheel to pick what to watch.", "/movie/spin-the-wheel"),
            ("Movie", "Search") => new PageMetadata("Movie Search | Eat My Movies", "Search for a movie by title and explore its details.", NoIndex: true),
            _ => new PageMetadata(existingTitle ?? "Eat My Movies")
        };
    }

    private PageMetadata Public(string title, string description, string path) =>
        new(title, description, AbsoluteUrl(path), AbsoluteUrl("/brand/bitten-o-red-my.png"), "Eat My Movies");

    private static string Compact(string value, int limit)
    {
        var text = Regex.Replace(value, @"\s+", " ").Trim();
        if (text.Length <= limit) return text;
        var end = text.LastIndexOf(' ', limit);
        return text[..(end > 0 ? end : limit)].TrimEnd() + "…";
    }
}
