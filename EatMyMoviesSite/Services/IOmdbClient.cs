namespace EatMyMoviesSite.Services
{
    internal interface IOmdbClient
    {
        Task<decimal?> GetImdbRatingAsync(string movieTitle);
    }
}
