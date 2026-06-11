using System.ComponentModel.DataAnnotations;

namespace EatMyMoviesSite.Options
{
    public sealed class OmdbOptions : IValidatableObject
    {
        public const string SectionName = "Omdb";

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        public Uri BaseUrl { get; set; } = new("https://www.omdbapi.com/");

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!BaseUrl.IsAbsoluteUri)
            {
                yield return new ValidationResult(
                    "OMDb base URL must be absolute.",
                    new[] { nameof(BaseUrl) });
            }

            if (Timeout <= TimeSpan.Zero)
            {
                yield return new ValidationResult(
                    "OMDb timeout must be greater than zero.",
                    new[] { nameof(Timeout) });
            }
        }
    }
}
