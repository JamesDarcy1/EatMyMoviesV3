using EatMyMovies.DataAccess;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Services;
using Microsoft.EntityFrameworkCore;

namespace EatMyMoviesSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureAppConfiguration(builder, args);
            ValidateRequiredConfiguration(builder.Configuration);

            var connectionString = builder.Configuration.GetConnectionString("DbConnection");

			builder.Services.AddDbContext<EatMyMoviesContext>(options =>
						options.UseSqlServer(connectionString));

			builder.Services.AddControllersWithViews();
            ConfigureServices(builder.Services);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append(
                        "Cache-Control", "public,max-age=31536000");
                }
            });

            app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapFallbackToController("Index", "Home");
			});

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

        private static void ValidateRequiredConfiguration(IConfiguration configuration)
        {
            var requiredKeys = new[]
            {
                "ConnectionStrings:DbConnection",
                "Tmdb:ApiKey",
                "Omdb:ApiKey"
            };

            var missingKeys = requiredKeys
                .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
                .ToList();

            if (missingKeys.Any())
            {
                throw new InvalidOperationException(
                    $"Missing required configuration value(s): {string.Join(", ", missingKeys)}.");
            }
        }

		private static void ConfigureServices(IServiceCollection services)
		{
            services.AddScoped<IRankingRepository, RankingRepository>();
            services.AddScoped<IListRepository, ListRepository>();
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<IMovieService, MovieService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddMemoryCache();
            services.AddHttpClient();
		}
	}
}
