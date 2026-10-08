using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieOfTheWeekEditorialNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EditorialNote",
                table: "MovieOfTheWeekSelections",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditorialNote",
                table: "MovieOfTheWeekSelections");
        }
    }
}
