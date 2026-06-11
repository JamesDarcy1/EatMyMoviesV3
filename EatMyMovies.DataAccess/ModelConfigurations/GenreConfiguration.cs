using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
    {
        public void Configure(EntityTypeBuilder<Genre> builder)
        {
            builder.Property(genre => genre.Name)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(genre => genre.Name)
                .IsUnique();
        }
    }
}
