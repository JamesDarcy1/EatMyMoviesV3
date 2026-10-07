using System.ComponentModel.DataAnnotations;

namespace EatMyMoviesSite.Options;

public sealed class SeoOptions : IValidatableObject
{
    public const string SectionName = "Seo";
    public string PublicOrigin { get; set; } = "https://eatmymovies.com";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host) ||
            uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 ||
            uri.UserInfo.Length > 0 || !uri.IsDefaultPort)
        {
            yield return new ValidationResult("Seo:PublicOrigin must be an HTTPS origin without a path, credentials, query, fragment or custom port.", [nameof(PublicOrigin)]);
        }
    }
}
