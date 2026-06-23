namespace EatMyMovies.DataAccess.QueryModels
{
    public sealed record AdminListSummary(Guid ListId, string Name, string Description, int MovieCount);
}
