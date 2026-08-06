using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAngebotSapKommentare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SapFuehrendKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SapSparteKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SapFuehrendKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "SapSparteKommentar",
                table: "Angebote");
        }
    }
}
