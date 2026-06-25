using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieOfTheWeekSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovieOfTheWeekSelections",
                columns: table => new
                {
                    MovieOfTheWeekSelectionId = table.Column<int>(type: "int", nullable: false),
                    MovieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieOfTheWeekSelections", x => x.MovieOfTheWeekSelectionId);
                    table.CheckConstraint("CK_MovieOfTheWeekSelections_SingletonId", "[MovieOfTheWeekSelectionId] = 1");
                    table.ForeignKey(
                        name: "FK_MovieOfTheWeekSelections_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "MovieId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovieOfTheWeekSelections_MovieId",
                table: "MovieOfTheWeekSelections",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovieOfTheWeekSelections");
        }
    }
}
