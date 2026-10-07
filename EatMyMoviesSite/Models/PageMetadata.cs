namespace EatMyMoviesSite.Models;

public sealed record PageMetadata(
    string Title,
    string? Description = null,
    string? CanonicalUrl = null,
    string? ImageUrl = null,
    string? ImageAlt = null,
    bool NoIndex = false,
    bool IsHome = false);
