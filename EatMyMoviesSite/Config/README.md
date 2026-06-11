# EatMyMovies Configuration

Do not commit real API keys or database connection strings to the JSON files in this folder.

The application requires these configuration values:

- `ConnectionStrings:DbConnection`
- `Tmdb:ApiKey`
- `Omdb:ApiKey`

TMDb and OMDb keys are bound through typed options and validated on startup. Optional typed option values can override defaults without changing the required secret names:

- `Tmdb:MaxRetryAttempts` defaults to `3`
- `Tmdb:Timeout` defaults to `00:00:30`
- `Omdb:BaseUrl` defaults to `https://www.omdbapi.com/`
- `Omdb:Timeout` defaults to `00:00:30`
- `MovieExternalApis:ExternalApiConcurrency` defaults to `4`
- `MovieExternalApis:SearchDropdownLimit` defaults to `5`

## Local development

Store local values in .NET user secrets for the web project:

```powershell
dotnet user-secrets init --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "ConnectionStrings:DbConnection" "<connection-string>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "Tmdb:ApiKey" "<tmdb-key>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet user-secrets set "Omdb:ApiKey" "<omdb-key>" --project EatMyMoviesSite\EatMyMoviesSite.csproj
```

Local development can continue to use the shared Azure SQL database by putting that connection string in user secrets. Developers can also use a local SQL Server connection string instead.

## Azure App Service

Configure production values in Azure App Service Configuration or deployment secrets. Use double underscores for nested keys:

- `ConnectionStrings__DbConnection`
- `Tmdb__ApiKey`
- `Omdb__ApiKey`

Use double underscores for optional nested option overrides too, for example `Tmdb__Timeout` or `MovieExternalApis__ExternalApiConcurrency`.

Existing checked-in secrets should be treated as exposed and rotated outside this repository change.
