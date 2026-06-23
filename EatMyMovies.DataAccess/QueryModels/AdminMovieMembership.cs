namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record AdminMovieMembership(Guid MovieId, Guid ListId, string ListName, int Ranking);
}
