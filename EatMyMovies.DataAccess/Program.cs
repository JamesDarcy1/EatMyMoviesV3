using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EatMyMovies.DataAccess
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

			var app = builder.Build();
			app.Run();
		}

		private static void ConfigureAppConfiguration(WebApplicationBuilder builder, string[] args)
		{
			var configurationBuilder = (IConfigurationBuilder)builder.Configuration;
			var siteConfigRoot = GetSiteConfigRoot(builder.Environment.ContentRootPath);
			configurationBuilder.Sources.Clear();

			configurationBuilder
				.SetBasePath(siteConfigRoot)
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

		private static string GetSiteConfigRoot(string contentRootPath)
		{
			if (Directory.Exists(Path.Combine(contentRootPath, "Config")))
			{
				return contentRootPath;
			}

			var siblingSitePath = Path.GetFullPath(Path.Combine(contentRootPath, "..", "EatMyMoviesSite"));
			if (Directory.Exists(Path.Combine(siblingSitePath, "Config")))
			{
				return siblingSitePath;
			}

			var childSitePath = Path.GetFullPath(Path.Combine(contentRootPath, "EatMyMoviesSite"));
			if (Directory.Exists(Path.Combine(childSitePath, "Config")))
			{
				return childSitePath;
			}

			return siblingSitePath;
		}

		private static void ValidateRequiredConfiguration(IConfiguration configuration)
		{
			var requiredKeys = new[]
			{
				"ConnectionStrings:DbConnection"
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
	}
}
