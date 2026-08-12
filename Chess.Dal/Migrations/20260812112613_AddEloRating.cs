using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chess.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddEloRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EloRating",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 1200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EloRating",
                table: "Players");
        }
    }
}
