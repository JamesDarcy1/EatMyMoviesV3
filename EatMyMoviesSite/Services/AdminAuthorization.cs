namespace EatMyMoviesSite.Services
{
    internal static class AdminAuthorization
    {
        public const string PolicyName = "AdminOnly";
        public const string ClaimType = "Admin";
        public const string ClaimValue = "true";
    }
}
