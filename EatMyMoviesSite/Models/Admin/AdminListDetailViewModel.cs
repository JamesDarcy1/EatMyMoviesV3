using EatMyMovies.DataAccess.QueryModels;
using EatMyMoviesSite.DTOs;

namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminListDetailViewModel
    {
        public Guid ListId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? TmdbQuery { get; set; }

        public int NextRanking => Movies.Count + 1;

        public IReadOnlyList<AdminListMovieRow> Movies { get; set; } = Array.Empty<AdminListMovieRow>();

        public IReadOnlyList<MovieDropdown> TmdbSearchResults { get; set; } = Array.Empty<MovieDropdown>();
    }
}
