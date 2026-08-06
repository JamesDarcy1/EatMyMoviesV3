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

        public string WatchProviderRegion { get; set; } = "GB";

        public TimeSpan MovieCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan TrailerCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan TrailerFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan CreditsCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan CreditsFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

        public TimeSpan ImdbRatingCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan UnknownImdbRatingCacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan WatchProviderCacheDuration { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan UnknownWatchProviderCacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan WatchProviderFailureCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WatchProviderRegion) ||
                WatchProviderRegion.Length != 2 ||
                WatchProviderRegion.Any(character => !char.IsAsciiLetter(character)))
            {
                yield return new ValidationResult(
                    "WatchProviderRegion must be a two-letter ISO 3166-1 country code.",
                    new[] { nameof(WatchProviderRegion) });
            }

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
            yield return (WatchProviderCacheDuration, nameof(WatchProviderCacheDuration));
            yield return (UnknownWatchProviderCacheDuration, nameof(UnknownWatchProviderCacheDuration));
            yield return (WatchProviderFailureCacheDuration, nameof(WatchProviderFailureCacheDuration));
        }
    }
}
