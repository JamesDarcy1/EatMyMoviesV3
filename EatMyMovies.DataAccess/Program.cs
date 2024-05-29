using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Configuration.AddJsonFile("Config/appsettings.json");

			builder.Services.AddDbContext<EatMyMoviesContext>(options =>
						options.UseSqlServer(
							builder.Configuration.GetConnectionString("DbConnection")));

			var app = builder.Build();
			app.Run();
		}
	}
}
