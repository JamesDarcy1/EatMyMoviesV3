using EatMyMovies.DataAccess;
using EatMyMovies.DataAccess.Repositories;
using EatMyMoviesSite.Options;
using EatMyMoviesSite.Services;
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
            ValidateDbConnectionString(builder.Configuration);

            var connectionString = builder.Configuration.GetConnectionString("DbConnection");

			builder.Services.AddDbContext<EatMyMoviesContext>(options =>
						options.UseSqlServer(connectionString));

			builder.Services.AddControllersWithViews();
            ConfigureServices(builder.Services, builder.Configuration);

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

        private static void ValidateDbConnectionString(IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DbConnection")))
            {
                throw new InvalidOperationException("Missing required configuration value: ConnectionStrings:DbConnection.");
            }
        }

		private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
		{
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

            services.AddScoped<IRankingRepository, RankingRepository>();
            services.AddScoped<IListRepository, ListRepository>();
            services.AddScoped<IMovieRepository, MovieRepository>();
            services.AddScoped<ITmdbMovieClient, TmdbMovieClient>();
            services.AddScoped<IOmdbClient, OmdbClient>();
            services.AddScoped<IMovieService, MovieService>();
            services.AddScoped<IStorageService, StorageService>();
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
