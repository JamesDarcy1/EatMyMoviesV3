namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record AdminListMovieRow(Guid MovieId, string Title, int? TmdbId, int Ranking);
}
