using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;

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
	}
}
