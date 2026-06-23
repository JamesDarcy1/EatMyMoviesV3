using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminListsViewModel
    {
        public IReadOnlyList<AdminListSummary> Lists { get; set; } = Array.Empty<AdminListSummary>();
    }
}
