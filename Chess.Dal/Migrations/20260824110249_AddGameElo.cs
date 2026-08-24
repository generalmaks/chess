using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chess.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddGameElo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackEloAfter",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BlackEloBefore",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhiteEloAfter",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhiteEloBefore",
                table: "Games",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackEloAfter",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BlackEloBefore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "WhiteEloAfter",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "WhiteEloBefore",
                table: "Games");
        }
    }
}
