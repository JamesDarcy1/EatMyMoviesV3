using EatMyMovies.DataAccess.QueryModels;
using DataList = EatMyMovies.DataAccess.Models.List;

namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminMovieDetailViewModel
    {
        public Guid MovieId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? TmdbId { get; set; }

        public IReadOnlyList<AdminMovieMembership> Memberships { get; set; } = Array.Empty<AdminMovieMembership>();

        public IReadOnlyList<DataList> Lists { get; set; } = Array.Empty<DataList>();
    }
}
