using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAngebotKommentare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IncotermKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KundeKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersandbedingungKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZahlungsbedingungenKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncotermKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "KundeKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "VersandbedingungKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "ZahlungsbedingungenKommentar",
                table: "Angebote");
        }
    }
}
