using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class ListConfiguration : IEntityTypeConfiguration<List>
    {
        public void Configure(EntityTypeBuilder<List> builder)
        {
            builder.Property(list => list.Name)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(list => list.Name)
                .IsUnique();
        }
    }
}
