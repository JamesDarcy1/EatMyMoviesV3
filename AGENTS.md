# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

EatMyMoviesV3 is a .NET 6 ASP.NET Core MVC application backed by Entity Framework Core and SQL Server.

- `EatMyMoviesV3.sln` is the solution entry point.
- `EatMyMoviesSite/` contains the MVC web app, Razor views, services, DTOs, static assets, and app configuration.
- `EatMyMovies.DataAccess/` contains EF Core models, `EatMyMoviesContext`, repositories, and migrations.
- There is currently no dedicated test project in the solution.

The application integrates with TMDb and OMDb via `TMDbLib`, `OMDbSharp`, and `RestSharp`. Movie/list persistence is handled through repositories registered in `EatMyMoviesSite/Program.cs`.

## Common Commands

Run commands from the repository root unless noted otherwise.

```powershell
dotnet restore EatMyMoviesV3.sln
dotnet build EatMyMoviesV3.sln
dotnet run --project EatMyMoviesSite\EatMyMoviesSite.csproj --launch-profile Development
```

Development launch URLs are defined in `EatMyMoviesSite/Properties/launchSettings.json`:

- `https://localhost:7018`
- `http://localhost:5065`

## Architecture Notes

- Controllers live in `EatMyMoviesSite/Controllers`.
- Service interfaces and implementations live in `EatMyMoviesSite/Services`.
- Data access should go through repository interfaces in `EatMyMovies.DataAccess/Repositories`.
- EF entities live in `EatMyMovies.DataAccess/Models`.
- Razor pages live under `EatMyMoviesSite/Views`.
- Shared static CSS/JS and assets live under `EatMyMoviesSite/wwwroot`.

Keep controller actions thin where practical. Put application logic in services and persistence logic in repositories, matching the current project shape.

## Configuration and Secrets

Configuration files live in `EatMyMoviesSite/Config`.

Be careful with secrets. The existing config files include API keys and a SQL Server connection string. Do not add new secrets to source-controlled files. Prefer environment variables, user secrets, or deployment-specific configuration when adding new sensitive values.

`Program.cs` loads:

- `Config/appsettings.json`
- `Config/appsettings.{ASPNETCORE_ENVIRONMENT}.json`

The app expects:

- `ConnectionStrings:DbConnection`
- `Tmdb:ApiKey`
- `Omdb:ApiKey`

## Database and EF Core

The EF Core context is `EatMyMovies.DataAccess/EatMyMoviesContext.cs`.

Migrations are stored in `EatMyMovies.DataAccess/Migrations`. If adding or changing persisted models, add a migration from the repository root:

```powershell
dotnet ef migrations add <MigrationName> --project EatMyMovies.DataAccess\EatMyMovies.DataAccess.csproj --startup-project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet ef database update --project EatMyMovies.DataAccess\EatMyMovies.DataAccess.csproj --startup-project EatMyMoviesSite\EatMyMoviesSite.csproj
```

Only run `database update` against the intended database. Check the active connection string first.

## Frontend Conventions

The UI uses Razor views, Bulma CSS from `wwwroot/lib/bulma`, jQuery, and project-specific styles in `EatMyMoviesSite/wwwroot/css/site.css`.

When changing the frontend:

- Keep styles consistent with the existing red/cream movie-themed visual language.
- Reuse Bulma classes and existing CSS utilities before introducing new patterns.
- Check both desktop and mobile breakpoints; `site.css` has explicit rules around `768px`.
- Keep static assets under `wwwroot`.

## Coding Conventions

- Preserve the existing C# style in nearby files.
- Use dependency injection registrations in `Program.cs` for new services/repositories.
- Prefer async all the way through when calling TMDb/OMDb or other I/O.
- Avoid bypassing repository interfaces from controllers.
- Keep DTO/view model mapping in `Mapper.cs` or nearby mapping helpers where that matches the existing pattern.
- Do not introduce broad refactors while making small feature or bug-fix changes.

## Verification

Before handing off changes, run at least:

```powershell
dotnet build EatMyMoviesV3.sln
```

For UI or routing changes, also run the site with the Development launch profile and manually verify the relevant page(s).

Because there is no test project today, note any behavior that was only manually verified.

## Git Notes

This workspace may be opened under a sandbox user that Git considers a different owner. If `git status` reports a dubious ownership warning, do not change global Git config unless the user approves it. Continue with filesystem inspection where possible.

Never discard user changes unless explicitly asked.
