# EatMyMovies Configuration

Do not commit real API keys, database connection strings, admin usernames, or admin password hashes to the JSON files in this folder.

The application requires these configuration values:

- `ConnectionStrings:DbConnection`
- `Tmdb:ApiKey`
- `Omdb:ApiKey`
- `AdminAuth:Username`
- `AdminAuth:PasswordHash`

TMDb, OMDb, and admin authentication settings are bound through typed options and validated on startup. Optional typed option values can override defaults without changing the required secret names:

- `Tmdb:MaxRetryAttempts` defaults to `3`
- `Tmdb:Timeout` defaults to `00:00:30`
- `Omdb:BaseUrl` defaults to `https://www.omdbapi.com/`
- `Omdb:Timeout` defaults to `00:00:30`
- `MovieExternalApis:ExternalApiConcurrency` defaults to `4`
- `MovieExternalApis:SearchDropdownLimit` defaults to `5`
- `MovieExternalApis:WatchProviderRegion` defaults to `GB`
- `MovieExternalApis:WatchProviderCacheDuration` defaults to `06:00:00`
- `MovieExternalApis:UnknownWatchProviderCacheDuration` defaults to `00:30:00`
- `MovieExternalApis:WatchProviderFailureCacheDuration` defaults to `00:15:00`

Movie detail pages use TMDb's JustWatch-powered watch-provider data for the configured region. The current product scope is United Kingdom only and displays at most three combined subscription, free, or ad-supported providers; rent and buy offers are not shown. JustWatch attribution must remain visible wherever this availability data is displayed.

## Local development

Store local values in .NET user secrets for the web project:

```powershell
dotnet user-secrets init --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "ConnectionStrings:DbConnection" "<connection-string>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "Tmdb:ApiKey" "<tmdb-key>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "Omdb:ApiKey" "<omdb-key>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "AdminAuth:Username" "<admin-username>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "AdminAuth:PasswordHash" "<password-hash>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
```

Local development can continue to use the shared Azure SQL database by putting that connection string in user secrets. Developers can also use a local SQL Server connection string instead.

Admin login is available at `/admin/login`. `AdminAuth:PasswordHash` uses this format:

```text
pbkdf2-sha256:<iterations>:<saltBase64>:<hashBase64>
```

You can generate a hash with PowerShell:

```powershell
$password = Read-Host -AsSecureString "Admin password"
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
$salt = New-Object byte[] 16
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($salt)
$iterations = 100000
$derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($plain, $salt, $iterations, [Security.Cryptography.HashAlgorithmName]::SHA256)
$hash = $derive.GetBytes(32)
"pbkdf2-sha256:${iterations}:$([Convert]::ToBase64String($salt)):$([Convert]::ToBase64String($hash))"
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
```

## Search metadata and canonical URLs

`Seo:PublicOrigin` defaults to `https://eatmymovies.com`. An override must contain only an HTTPS origin (no credentials, custom port, path, query or fragment). Use `Seo__PublicOrigin` in Azure App Service. Public canonical URLs, Open Graph URLs and recommendation sharing use this value instead of the incoming Host header.

In Production, requests on the known `www.eatmymovies.com` alias receive a method-preserving 308 redirect to the configured origin. Localhost and staging hosts are not redirected to production. Keep the existing IIS/Azure trusted proxy integration for HTTPS scheme detection; do not enable blanket trust of forwarded headers. The application retains HTTPS redirection after the hostname redirect.

After deployment, check HTTP/HTTPS and www/non-www variants for correct redirects and no loops, inspect home/list/movie URLs in Search Console, and monitor indexing errors and click-through rates after recrawling. Metadata influences Google's presentation but does not guarantee a particular snippet or ranking. No deployment or Search Console changes are performed by the SEO tests.

The XML sitemap is served at `/sitemap.xml` and is listed in `/robots.txt`; it uses `Seo:PublicOrigin`. Once the reviewed release is live and the database migrations have run, submit that URL in Search Console. Record Page indexing counts and Search results queries, impressions, clicks and click-through rate for a 28-day baseline and the next comparable 28-day period. Capture mobile PageSpeed Insights reports for home, a list, a detail page and the recommender with the URLs and measurement date. Apply `AddMovieOfTheWeekEditorialNote` and `PublishApprovedListIntroductions` only to the intended database after checking the active connection string. The latter updates the nine list introductions only if their prior descriptions still exactly match the 8 October 2026 live copy.

## Azure App Service settings

Configure production values in Azure App Service Configuration or deployment secrets. Use double underscores for nested keys:

- `ConnectionStrings__DbConnection`
- `Tmdb__ApiKey`
- `Omdb__ApiKey`
- `AdminAuth__Username`
- `AdminAuth__PasswordHash`

Use double underscores for optional nested option overrides too, for example `Tmdb__Timeout` or `MovieExternalApis__ExternalApiConcurrency`.

Existing checked-in secrets should be treated as exposed and rotated outside this repository change.
