namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record ListPageMovie(Guid MovieId, string Title, int? TmdbId, int Ranking);
}
