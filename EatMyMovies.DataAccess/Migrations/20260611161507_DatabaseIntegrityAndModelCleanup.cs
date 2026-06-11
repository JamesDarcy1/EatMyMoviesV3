using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatMyMovies.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntegrityAndModelCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Movies] WHERE [Title] IS NULL OR LTRIM(RTRIM([Title])) = '')
                    THROW 51000, 'Cannot add movie title constraints because Movies contains blank titles.', 1;

                IF EXISTS (SELECT [Title] FROM [Movies] GROUP BY [Title] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique movie title constraint because Movies contains duplicate titles.', 1;

                IF EXISTS (SELECT 1 FROM [Movies] WHERE [TmdbId] <= 0)
                    THROW 51000, 'Cannot add TMDb ID constraints because Movies contains non-positive TMDb IDs.', 1;

                IF EXISTS (SELECT [TmdbId] FROM [Movies] WHERE [TmdbId] IS NOT NULL GROUP BY [TmdbId] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique TMDb ID constraint because Movies contains duplicate TMDb IDs.', 1;

                IF EXISTS (SELECT 1 FROM [Lists] WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = '')
                    THROW 51000, 'Cannot add list name constraints because Lists contains blank names.', 1;

                IF EXISTS (SELECT [Name] FROM [Lists] GROUP BY [Name] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique list name constraint because Lists contains duplicate names.', 1;

                IF EXISTS (SELECT 1 FROM [Genres] WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = '')
                    THROW 51000, 'Cannot add genre name constraints because Genres contains blank names.', 1;

                IF EXISTS (SELECT 1 FROM [Genres] WHERE LEN([Name]) > 450)
                    THROW 51000, 'Cannot add genre name constraints because Genres contains names longer than 450 characters.', 1;

                IF EXISTS (SELECT [Name] FROM [Genres] GROUP BY [Name] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique genre name constraint because Genres contains duplicate names.', 1;

                IF EXISTS (SELECT 1 FROM [ListRankings] WHERE [Ranking] <= 0)
                    THROW 51000, 'Cannot add ranking constraints because ListRankings contains non-positive rankings.', 1;

                IF EXISTS (SELECT [ListId], [Ranking] FROM [ListRankings] GROUP BY [ListId], [Ranking] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique list/ranking constraint because ListRankings contains duplicate rank slots.', 1;

                IF EXISTS (SELECT [ListId], [MovieId] FROM [ListRankings] GROUP BY [ListId], [MovieId] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add unique list/movie constraint because ListRankings contains duplicate movie memberships.', 1;

                IF EXISTS (SELECT [MovieId], [GenreId] FROM [MovieGenres] GROUP BY [MovieId], [GenreId] HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot add movie/genre composite key because MovieGenres contains duplicate links.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Movies_Title",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres");

            migrationBuilder.DropIndex(
                name: "IX_MovieGenres_MovieId",
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

            migrationBuilder.DropColumn(
                name: "MovieGenreId",
                table: "MovieGenres");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Genres",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres",
                columns: new[] { "MovieId", "GenreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Title",
                table: "Movies",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true,
                filter: "[TmdbId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movies_TmdbId_Positive",
                table: "Movies",
                sql: "[TmdbId] IS NULL OR [TmdbId] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_Name",
                table: "Lists",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListRankings_ListId_MovieId",
                table: "ListRankings",
                columns: new[] { "ListId", "MovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListRankings_ListId_Ranking",
                table: "ListRankings",
                columns: new[] { "ListId", "Ranking" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ListRankings_Ranking_Positive",
                table: "ListRankings",
                sql: "[Ranking] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movies_TmdbId_Positive",
                table: "Movies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MovieGenres",
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_ListRankings_Ranking_Positive",
                table: "ListRankings");

            migrationBuilder.DropIndex(
                name: "IX_Genres_Name",
                table: "Genres");

            migrationBuilder.AddColumn<Guid>(
                name: "MovieGenreId",
                table: "MovieGenres",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Genres",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MovieGenres",
                table: "MovieGenres",
                column: "MovieGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Title",
                table: "Movies",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_MovieId",
                table: "MovieGenres",
                column: "MovieId");

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
    }
}
