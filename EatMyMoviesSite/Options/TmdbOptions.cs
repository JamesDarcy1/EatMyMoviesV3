using System.ComponentModel.DataAnnotations;

namespace EatMyMoviesSite.Options
{
    public sealed class TmdbOptions : IValidatableObject
    {
        public const string SectionName = "Tmdb";

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Range(1, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Timeout <= TimeSpan.Zero)
            {
                yield return new ValidationResult(
                    "TMDb timeout must be greater than zero.",
                    new[] { nameof(Timeout) });
            }
        }
    }
}
