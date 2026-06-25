using EatMyMoviesSite.DTOs;

namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminMovieOfTheWeekViewModel
    {
        public string? CurrentTitle { get; set; }

        public int? CurrentTmdbId { get; set; }

        public DateTime? UpdatedUtc { get; set; }

        public string? TmdbQuery { get; set; }

        public IReadOnlyList<MovieDropdown> TmdbSearchResults { get; set; } = Array.Empty<MovieDropdown>();
    }
}
