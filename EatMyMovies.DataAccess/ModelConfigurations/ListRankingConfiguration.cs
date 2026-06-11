using EatMyMovies.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EatMyMovies.DataAccess.ModelConfigurations
{
    internal sealed class ListRankingConfiguration : IEntityTypeConfiguration<ListRanking>
    {
        public void Configure(EntityTypeBuilder<ListRanking> builder)
        {
            builder.HasOne(listRanking => listRanking.List)
                .WithMany(list => list.ListRankings)
                .HasForeignKey(listRanking => listRanking.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(listRanking => listRanking.Movie)
                .WithMany(movie => movie.ListRankings)
                .HasForeignKey(listRanking => listRanking.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(listRanking => new { listRanking.ListId, listRanking.Ranking })
                .IsUnique();

            builder.HasIndex(listRanking => new { listRanking.ListId, listRanking.MovieId })
                .IsUnique();

            builder.HasIndex(listRanking => listRanking.MovieId);

            builder.ToTable(table => table.HasCheckConstraint("CK_ListRankings_Ranking_Positive", "[Ranking] > 0"));
        }
    }
}
