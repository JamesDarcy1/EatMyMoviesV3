namespace EatMyMoviesSite.DTOs
{
    public sealed class WatchAvailability
    {
        public string? Link { get; set; }

        public IReadOnlyList<WatchProviderOption> Providers { get; set; } = Array.Empty<WatchProviderOption>();
    }
}
