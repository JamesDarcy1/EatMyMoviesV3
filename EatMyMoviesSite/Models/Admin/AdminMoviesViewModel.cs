using EatMyMovies.DataAccess.QueryModels;

namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminMoviesViewModel
    {
        public string? SearchTerm { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public IReadOnlyList<AdminMovieSummary> Movies { get; set; } = Array.Empty<AdminMovieSummary>();
    }
}
