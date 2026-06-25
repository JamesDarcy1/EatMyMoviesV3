using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EatMyMovies.DataAccess
{
    public class EatMyMoviesContext : DbContext
    {
        public EatMyMoviesContext(DbContextOptions<EatMyMoviesContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<List> Lists { get; set; }
        public DbSet<ListRanking> ListRankings { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<MovieGenre> MovieGenres { get; set; }
        public DbSet<MovieOfTheWeekSelection> MovieOfTheWeekSelections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EatMyMoviesContext).Assembly);
        }
    }
}
