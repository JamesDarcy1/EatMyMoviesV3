using System.ComponentModel.DataAnnotations;

namespace EatMyMoviesSite.Options
{
    public sealed class MovieExternalApiOptions : IValidatableObject
    {
        public const string SectionName = "MovieExternalApis";

        [Range(1, 20)]
        public int ExternalApiConcurrency { get; set; } = 4;

        [Range(1, 20)]
        public int SearchDropdownLimit { get; set; } = 5;

        public TimeSpan MovieCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan TrailerCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan TrailerFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan CreditsCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan CreditsFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan ImdbRatingCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan UnknownImdbRatingCacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var (value, name) in GetDurations())
            {
                if (value <= TimeSpan.Zero)
                {
                    yield return new ValidationResult(
                        $"{name} must be greater than zero.",
                        new[] { name });
                }
            }
        }

        private IEnumerable<(TimeSpan Value, string Name)> GetDurations()
        {
            yield return (MovieCacheDuration, nameof(MovieCacheDuration));
            yield return (TrailerCacheDuration, nameof(TrailerCacheDuration));
            yield return (TrailerFailureCacheDuration, nameof(TrailerFailureCacheDuration));
            yield return (CreditsCacheDuration, nameof(CreditsCacheDuration));
            yield return (CreditsFailureCacheDuration, nameof(CreditsFailureCacheDuration));
            yield return (ImdbRatingCacheDuration, nameof(ImdbRatingCacheDuration));
            yield return (UnknownImdbRatingCacheDuration, nameof(UnknownImdbRatingCacheDuration));
        }
    }
}
