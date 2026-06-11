using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AsyncRepositoryQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres");

            migrationBuilder.DropIndex(
                name: "IX_ListRankings_ListId",
                table: "ListRankings");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Movies",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Lists",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Title",
                table: "Movies",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId_MovieId",
                table: "MovieGenres",
                columns: new[] { "GenreId", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lists_Name",
                table: "Lists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ListRankings_ListId_MovieId",
                table: "ListRankings",
                columns: new[] { "ListId", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_ListRankings_ListId_Ranking",
                table: "ListRankings",
                columns: new[] { "ListId", "Ranking" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movies_Title",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_MovieGenres_GenreId_MovieId",
                table: "MovieGenres");

            migrationBuilder.DropIndex(
                name: "IX_Lists_Name",
                table: "Lists");

            migrationBuilder.DropIndex(
                name: "IX_ListRankings_ListId_MovieId",
                table: "ListRankings");

            migrationBuilder.DropIndex(
                name: "IX_ListRankings_ListId_Ranking",
                table: "ListRankings");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Lists",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_ListRankings_ListId",
                table: "ListRankings",
                column: "ListId");
        }
    }
}
