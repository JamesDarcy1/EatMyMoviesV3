# Frontend Vendor Assets

This app intentionally vendors browser dependencies under `wwwroot/lib` and does not use npm, Vite, Webpack, LibMan, or CDN-only runtime dependencies.

## Current Assets

| Asset | Version | Local path | Source | License |
| --- | --- | --- | --- | --- |
| Bulma | 1.0.4 | `bulma/css/` | https://github.com/jgthms/bulma | MIT |
| jQuery | 3.7.1 | `jquery/dist/` | https://github.com/jquery/jquery | MIT |
| jQuery Validation | 1.21.0 | `jquery-validation/dist/` | https://github.com/jquery-validation/jquery-validation | MIT |
| jQuery Validation Unobtrusive | 4.0.0 | `jquery-validation-unobtrusive/` | https://github.com/dotnet/aspnetcore | Apache-2.0 |
| Vue | 3.5.21 | `vue/` | https://github.com/vuejs/core | MIT |
| canvas-confetti | 1.9.4 | `canvas-confetti/` | https://github.com/catdad/canvas-confetti | ISC |

## Update Process

1. Download the upstream release artifact from the source project or official package tarball.
2. Replace only the files for that dependency under its existing `wwwroot/lib` folder.
3. Preserve readable and minified files when both are already present.
4. Preserve license files and update this table with the new version.
5. Run `dotnet build EatMyMoviesV3.sln` and `dotnet test EatMyMoviesV3.sln`.
6. For UI-affecting dependency changes, run the site with the Development launch profile and verify home, recommender, list, detail, and search pages at desktop and mobile widths.

## Notes

- Use readable assets in Development when the layout or page provides an environment-specific include.
- Use minified assets outside Development when a matching minified file exists.
- Keep the local `wwwroot/lib` model unless the project explicitly adopts a frontend package pipeline later.
