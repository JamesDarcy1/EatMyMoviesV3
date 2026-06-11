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
		public DbSet<Genre> Genres { get; set; } 
		public DbSet<MovieGenre> MovieGenres { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie>()
                .HasIndex(movie => movie.Title);

            modelBuilder.Entity<Movie>()
                .HasIndex(movie => movie.TmdbId);

            modelBuilder.Entity<List>()
                .HasIndex(list => list.Name);

            modelBuilder.Entity<ListRanking>()
                .HasOne(listRanking => listRanking.List)
                .WithMany()
                .HasForeignKey(listRanking => listRanking.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListRanking>()
                .HasOne(listRanking => listRanking.Movie)
                .WithMany()
                .HasForeignKey(listRanking => listRanking.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListRanking>()
                .HasIndex(listRanking => new { listRanking.ListId, listRanking.Ranking });

            modelBuilder.Entity<ListRanking>()
                .HasIndex(listRanking => new { listRanking.ListId, listRanking.MovieId });

            modelBuilder.Entity<ListRanking>()
                .HasIndex(listRanking => listRanking.MovieId);

            modelBuilder.Entity<MovieGenre>()
                .HasOne(movieGenre => movieGenre.Genre)
                .WithMany()
                .HasForeignKey(movieGenre => movieGenre.GenreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieGenre>()
                .HasOne(movieGenre => movieGenre.Movie)
                .WithMany()
                .HasForeignKey(movieGenre => movieGenre.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieGenre>()
                .HasIndex(movieGenre => new { movieGenre.GenreId, movieGenre.MovieId });

            modelBuilder.Entity<MovieGenre>()
                .HasIndex(movieGenre => movieGenre.MovieId);
        }
	}
}
