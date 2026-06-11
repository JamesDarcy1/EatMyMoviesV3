using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.Property(movie => movie.Title)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(movie => movie.Title)
                .IsUnique();

            builder.HasIndex(movie => movie.TmdbId)
                .IsUnique()
                .HasFilter("[TmdbId] IS NOT NULL");

            builder.ToTable(table => table.HasCheckConstraint("CK_Movies_TmdbId_Positive", "[TmdbId] IS NULL OR [TmdbId] > 0"));
        }
    }
}
