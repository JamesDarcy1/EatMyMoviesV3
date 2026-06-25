namespace EatMyMoviesSite.Models.Admin
{
    public sealed class AdminDashboardViewModel
    {
        public int MovieCount { get; set; }

        public int ListCount { get; set; }

        public int RankingCount { get; set; }

        public string? MovieOfTheWeekTitle { get; set; }

        public int? MovieOfTheWeekTmdbId { get; set; }
    }
}
