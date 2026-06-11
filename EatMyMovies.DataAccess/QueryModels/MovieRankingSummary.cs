namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record MovieRankingSummary(Guid MovieId, Guid ListId, string ListName, int Ranking, Guid ListRankingId);
}
