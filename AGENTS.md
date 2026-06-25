# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

EatMyMoviesV3 is a .NET 10 ASP.NET Core MVC application backed by Entity Framework Core and SQL Server. Nullable reference types and implicit usings are enabled across the projects.

- `EatMyMoviesV3.sln` is the solution entry point.
- `EatMyMoviesSite/` contains the MVC web app, Razor views, controllers, services, DTOs, mapping helpers, static assets, and app configuration.
- `EatMyMovies.DataAccess/` contains EF Core models, `EatMyMoviesContext`, repositories, and migrations.
- `EatMyMovies.Tests/` contains the xUnit test project.

`EatMyMovies.DataAccess` is a standard .NET class library. Keep web SDK, runtime identifier, self-contained, and other deployment settings in `EatMyMoviesSite`; adding them to the data project can create stale RID-specific reference assemblies during publish.

The application integrates with TMDb via `TMDbLib` and OMDb through thin external client services. Movie/list persistence is handled through repositories registered in `EatMyMoviesSite/Program.cs`.

## Keep This File Current

Treat this file as living agent guidance. When making code, structure, configuration, dependency, command, routing, test, or workflow changes, check whether `AGENTS.md` is now stale. Update it in the same change whenever the repository shape, required setup, important conventions, verification steps, or gotchas change.

Before handing off a substantial change, briefly compare the final codebase against this file and either update it or explicitly note that no AGENTS.md update was needed.

## Common Commands

Run commands from the repository root unless noted otherwise.

```powershell
dotnet restore EatMyMoviesV3.sln
dotnet build EatMyMoviesV3.sln
dotnet test EatMyMoviesV3.sln
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
- EF entity configuration classes live in `EatMyMovies.DataAccess/ModelConfigurations`.
- DTOs live in `EatMyMoviesSite/DTOs`.
- View models live in `EatMyMoviesSite/Models`.
- Enum types live in `EatMyMoviesSite/Enums`.
- Shared mapping logic lives in `EatMyMoviesSite/Mapper.cs`.
- Razor pages live under `EatMyMoviesSite/Views`.
- Shared static CSS/JS and assets live under `EatMyMoviesSite/wwwroot`.
- `EatMyMoviesSite/Languages.json` is used by `LanguageHelper` and is linked into the test project output.

Keep controller actions thin where practical. Put application logic in services and persistence logic in repositories, matching the current project shape. Use `Mapper.cs` or nearby mapping helpers for DTO/view model mapping when that matches the existing pattern.

Admin content management lives under `/admin` and is protected by the `AdminOnly` cookie-auth policy. Keep admin orchestration in `IAdminContentService`/`AdminContentService`; do not reintroduce public storage/write utility controllers for movie/list/ranking/movie-of-the-week edits. Admin POST actions should remain anti-forgery protected and use post/redirect/get.

`Program.cs` currently:

- loads JSON config, user secrets in Development, environment variables, and command-line args;
- validates required configuration on startup;
- binds typed options for TMDb, OMDb, admin auth, and movie external API defaults;
- configures cookie authentication and the `AdminOnly` authorization policy;
- registers repositories, TMDb/OMDb external clients, `IMovieService`, `IAdminContentService`, memory cache, and the named OMDb `HttpClient`;
- maps the default MVC route and falls back to `Home/Index`;
- serves static files, including a long-lived cache header for static asset responses.

`MovieService` owns movie caching, recommendation/list-building behavior, movie-of-the-week homepage composition, and movie detail composition. TMDb and OMDb transport logic belongs behind `ITmdbMovieClient` and `IOmdbClient`; keep external movie API orchestration in services rather than moving it into controllers.

`AdminContentService` owns content-management workflows such as creating/updating lists, searching stored movies, selecting TMDb movies by TMDb ID, adding movies to lists, moving rankings, removing list memberships while closing rank gaps, and setting or clearing the current Movie of the Week through `/admin/movie-of-the-week`.

## Configuration and Secrets

Configuration files live in `EatMyMoviesSite/Config`.

Be careful with secrets. Do not add API keys or connection strings to source-controlled files. Prefer environment variables, user secrets, or deployment-specific configuration when adding new sensitive values.

`Program.cs` loads:

- `Config/appsettings.json`
- `Config/appsettings.{ASPNETCORE_ENVIRONMENT}.json`
- .NET user secrets when `ASPNETCORE_ENVIRONMENT` is `Development`
- environment variables
- command-line arguments

The app expects:

- `ConnectionStrings:DbConnection`
- `Tmdb:ApiKey`
- `Omdb:ApiKey`
- `AdminAuth:Username`
- `AdminAuth:PasswordHash`

`TmdbOptions`, `OmdbOptions`, `AdminAuthOptions`, and `MovieExternalApiOptions` bind optional retry, timeout, cache, concurrency, and query-limit settings while preserving defaults when the optional keys are absent. Startup validates required API keys/admin credentials through options validation and validates the database connection string explicitly.

Use `EatMyMoviesSite/Config/README.md` as the source of truth for local user-secrets setup and Azure App Service environment variable names.

## Database and EF Core

The EF Core context is `EatMyMovies.DataAccess/EatMyMoviesContext.cs`.

Entity configuration is split into `IEntityTypeConfiguration<T>` classes under `EatMyMovies.DataAccess/ModelConfigurations`; keep `OnModelCreating` as assembly-based configuration loading unless there is a strong reason to do otherwise.

Repository reads should use async EF Core APIs, materialize bounded results inside the repository, and apply `AsNoTracking()` for read-only paths. Do not return `IQueryable` or lazily evaluated `IEnumerable` from repository interfaces. Use small projection records under `EatMyMovies.DataAccess/QueryModels` when services need summaries, list-page rows, or ranking context instead of tracked entity graphs.

`ListRanking` and `MovieGenre` expose explicit FK properties. Prefer FK/scalar filters for repository queries and reserve tracked entity queries for write paths that update or delete rows.

Movie/list/genre/list-ranking invariants are enforced by database constraints and unique indexes, not only repository checks. Keep list names, movie titles, non-null TMDb IDs, genre names, list rank slots, list movie memberships, and movie/genre links unique. Rankings and TMDb IDs must be positive. Use the transactional ranking repository methods for inserting, moving, or removing movies in lists so temporary reordering does not violate unique rank slots and removals close rank gaps.

`MovieOfTheWeekSelection` is a singleton table keyed by `MovieOfTheWeekSelectionId = 1` and points at a stored movie. Use `IMovieOfTheWeekRepository` to read, set, or clear the selection; do not reintroduce hardcoded homepage movie titles.

Migrations are stored in `EatMyMovies.DataAccess/Migrations`. If adding or changing persisted models, add a migration from the repository root:

```powershell
dotnet ef migrations add <MigrationName> --project EatMyMovies.DataAccess\EatMyMovies.DataAccess.csproj --startup-project EatMyMoviesSite\EatMyMoviesSite.csproj
dotnet ef database update --project EatMyMovies.DataAccess\EatMyMovies.DataAccess.csproj --startup-project EatMyMoviesSite\EatMyMoviesSite.csproj
```

Only run `database update` against the intended database. Check the active connection string first.

## Frontend Conventions

The UI uses Razor views, Bulma CSS from `wwwroot/lib/bulma`, jQuery, Vue from `wwwroot/lib/vue`, and project-specific styles/scripts in `EatMyMoviesSite/wwwroot/css/site.css` and `EatMyMoviesSite/wwwroot/js/site.js`.

When changing the frontend:

- Keep styles consistent with the existing red/cream movie-themed visual language.
- Treat the home page and recommender flow as the strongest current design reference. Preserve the midnight marquee/ticket-booth direction and extend it to supporting pages instead of redesigning those pages away from it.
- Reuse Bulma classes and existing CSS utilities before introducing new patterns.
- Check both desktop and mobile breakpoints; `site.css` has explicit rules around `768px`.
- Keep static assets under `wwwroot`.
- Preserve local vendored assets under `wwwroot/lib`; do not replace them with CDN-only dependencies unless the user explicitly wants that.
- `EatMyMoviesSite/wwwroot/lib/README.md` documents frontend vendor versions, source URLs, licenses, and the manual update process. Update it whenever vendored frontend assets change.
- Use readable vendor files in Development and minified vendor files outside Development where the layout or view has environment-specific includes.

## Coding Conventions

- Preserve the existing C# style in nearby files.
- Use dependency injection registrations in `Program.cs` for new services/repositories.
- Prefer async all the way through when calling TMDb/OMDb or other I/O.
- Avoid bypassing repository interfaces from controllers.
- Keep DTO/view model mapping in `Mapper.cs` or nearby mapping helpers where that matches the existing pattern.
- Use `IHttpClientFactory` for injected HTTP clients in app services; OMDb uses the named client registered by `OmdbClient.HttpClientName`.
- Use `IMemoryCache` consistently for TMDb/OMDb lookups where caching already exists.
- Keep test seams internal where useful; the web project exposes internals to `EatMyMovies.Tests`.
- Do not introduce broad refactors while making small feature or bug-fix changes.

## Verification

Before handing off changes, run at least:

```powershell
dotnet build EatMyMoviesV3.sln
dotnet test EatMyMoviesV3.sln
```

The current test suite covers repositories, `MovieService`, `AdminContentService`, mapper behavior, and list/admin controllers. Add or update focused tests when changing those areas.

For UI or routing changes, also run the site with the Development launch profile and manually verify the relevant page(s). Use the Development URLs listed above. Admin verification requires `AdminAuth:Username` and `AdminAuth:PasswordHash` to be present in user secrets or environment variables. Movie-of-the-week changes should verify `/`, `/admin`, and `/admin/movie-of-the-week`.

For frontend UX refreshes, manually verify the home page, recommender, movie list, movie detail, and search flows across desktop and mobile widths. Confirm refreshed supporting pages still feel consistent with the midnight marquee direction used by the home and recommender pages.

## Git Notes

This workspace may be opened under a sandbox user that Git considers a different owner. If `git status` reports a dubious ownership warning, do not change global Git config unless the user approves it. Continue with filesystem inspection where possible.

Never discard user changes unless explicitly asked.
