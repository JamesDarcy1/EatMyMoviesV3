namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record StoredMovieSummary(Guid MovieId, string Title, int? TmdbId);
}
