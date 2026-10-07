namespace EatMyMoviesSite.Services;

internal sealed class PublicUrlMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/", "/about", "/contact", "/movie/detail", "/movie/search", "/movie/recommender",
        "/movie/spin-the-wheel", "/list/top100",
        "/list/top-100", "/list/comedies", "/list/foreign-films", "/list/documentaries",
        "/list/christmas", "/list/standout-soundtracks", "/list/iconic-80s", "/list/disney", "/list/horrors"
    };

    public async Task InvokeAsync(HttpContext context, SeoMetadataService seo)
    {
        var request = context.Request;
        // Only this known production alias is redirected; local/staging hosts stay local.
        // Scheme detection remains with the host's trusted proxy integration / HTTPS middleware.
        if (environment.IsProduction() && request.Host.Host.Equals("www.eatmymovies.com", StringComparison.OrdinalIgnoreCase) &&
            !request.Host.Host.Equals(new Uri(seo.PublicOrigin).Host, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(seo.PublicOrigin + request.PathBase + request.Path + request.QueryString,
                permanent: true, preserveMethod: true);
            return;
        }

        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        {
            var path = request.Path.Value ?? "/";
            var trimmedPath = path.Length > 1 ? path.TrimEnd('/') : path;
            var target = trimmedPath.Equals("/home", StringComparison.OrdinalIgnoreCase) ||
                         trimmedPath.Equals("/home/index", StringComparison.OrdinalIgnoreCase)
                ? "/" : PublicPaths.Contains(trimmedPath) ? trimmedPath.ToLowerInvariant() : null;
            if (target != null && path != target)
            {
                context.Response.Redirect(request.PathBase + target + request.QueryString, permanent: true);
                return;
            }
        }
        await next(context);
    }
}
