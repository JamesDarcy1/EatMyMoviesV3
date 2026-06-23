namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record AdminMovieSummary(Guid MovieId, string Title, int? TmdbId, int ListCount);
}
