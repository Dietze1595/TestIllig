using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAuftragsanlageVolltextUndLiefertermin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LieferterminAngebot",
                table: "Auftragsbestaetigungen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LieferterminBestaetigung",
                table: "Auftragsbestaetigungen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "LieferterminIdentisch",
                table: "Auftragsbestaetigungen",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SonstigeAbweichungen",
                table: "Auftragsbestaetigungen",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Volltext",
                table: "Auftragsbestaetigungen",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Volltext",
                table: "Angebote",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LieferterminAngebot",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "LieferterminBestaetigung",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "LieferterminIdentisch",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "SonstigeAbweichungen",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "Volltext",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "Volltext",
                table: "Angebote");
        }
    }
}
