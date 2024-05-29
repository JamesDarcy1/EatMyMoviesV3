using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddImdbRatingToMovies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ImdbRating",
                table: "Movies",
                type: "decimal(3,1)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImdbRating",
                table: "Movies");
        }
    }
}
