using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class MovieOfTheWeekSelectionConfiguration : IEntityTypeConfiguration<MovieOfTheWeekSelection>
    {
        public void Configure(EntityTypeBuilder<MovieOfTheWeekSelection> builder)
        {
            builder.Property(selection => selection.MovieOfTheWeekSelectionId)
                .ValueGeneratedNever();

            builder.Property(selection => selection.UpdatedUtc)
                .IsRequired();

            builder.HasOne(selection => selection.Movie)
                .WithMany()
                .HasForeignKey(selection => selection.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(selection => selection.MovieId);

            builder.ToTable(table => table.HasCheckConstraint(
                "CK_MovieOfTheWeekSelections_SingletonId",
                "[MovieOfTheWeekSelectionId] = 1"));
        }
    }
}
