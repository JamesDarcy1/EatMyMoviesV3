using EatMyMovies.DataAccess;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EatMyMoviesSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureAppConfiguration(builder, args);
			builder.Services.AddDbContext<EatMyMoviesContext>((serviceProvider, options) =>
				options.UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("DbConnection")));

			builder.Services.AddControllersWithViews(options => options.Filters.Add<PublicPageExceptionFilter>());
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();
            ValidateDbConnectionString(app.Configuration);

            app.UseExceptionHandler("/error/500");
            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseMiddleware<PublicUrlMiddleware>();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    var versioned = ctx.Context.Request.Query.ContainsKey("v") ||
                        System.Text.RegularExpressions.Regex.IsMatch(
                            ctx.Context.Request.Path.Value ?? string.Empty, @"-v\d+(?:-|\.)");
                    ctx.Context.Response.Headers.CacheControl = versioned
                        ? "public,max-age=31536000,immutable"
                        : "public,max-age=86400";
                }
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

			app.Run();
        }

        private static void ConfigureAppConfiguration(WebApplicationBuilder builder, string[] args)
        {
            builder.Configuration
                .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"Config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>(optional: true);
            }

            builder.Configuration
                .AddEnvironmentVariables()
                .AddCommandLine(args);
        }

        private static void ValidateDbConnectionString(IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DbConnection")))
            {
                throw new InvalidOperationException("Missing required configuration value: ConnectionStrings:DbConnection.");
            }
        }

		private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
		{
            services.AddOptions<SeoOptions>()
                .Bind(configuration.GetSection(SeoOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<SeoMetadataService>();
            services.AddOptions<TmdbOptions>()
                .Bind(configuration.GetSection(TmdbOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<OmdbOptions>()
                .Bind(configuration.GetSection(OmdbOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<MovieExternalApiOptions>()
                .Bind(configuration.GetSection(MovieExternalApiOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<AdminAuthOptions>()
                .Bind(configuration.GetSection(AdminAuthOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "EatMyMovies.Admin";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.LoginPath = "/admin/login";
                    options.AccessDeniedPath = "/admin/login";
                    options.LogoutPath = "/admin/logout";
                    options.SlidingExpiration = true;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AdminAuthorization.PolicyName, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(AdminAuthorization.ClaimType, AdminAuthorization.ClaimValue);
                });
            });

            services.AddScoped<IRankingRepository, RankingRepository>();
            services.AddScoped<IListRepository, ListRepository>();
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IMovieOfTheWeekRepository, MovieOfTheWeekRepository>();
            services.AddScoped<ITmdbMovieClient, TmdbMovieClient>();
            services.AddScoped<IOmdbClient, OmdbClient>();
            services.AddScoped<IMovieService, MovieService>();
            services.AddScoped<IAdminContentService, AdminContentService>();
            services.AddMemoryCache();
            services.AddHttpClient(OmdbClient.HttpClientName, (serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OmdbOptions>>().Value;
                httpClient.BaseAddress = options.BaseUrl;
                httpClient.Timeout = options.Timeout;
            });
		}
	}
}
