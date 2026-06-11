using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class MovieGenreConfiguration : IEntityTypeConfiguration<MovieGenre>
    {
        public void Configure(EntityTypeBuilder<MovieGenre> builder)
        {
            builder.HasKey(movieGenre => new { movieGenre.MovieId, movieGenre.GenreId });

            builder.HasOne(movieGenre => movieGenre.Genre)
                .WithMany(genre => genre.MovieGenres)
                .HasForeignKey(movieGenre => movieGenre.GenreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(movieGenre => movieGenre.Movie)
                .WithMany(movie => movie.MovieGenres)
                .HasForeignKey(movieGenre => movieGenre.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(movieGenre => new { movieGenre.GenreId, movieGenre.MovieId });
        }
    }
}
