using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddStuecklistenpruefungVerlaufStand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErreichterSchritt",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "SapDateiname",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StuecklisteJson",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VergleichsErgebnisJson",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErreichterSchritt",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SapDateiname",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "StuecklisteJson",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "VergleichsErgebnisJson",
                table: "StuecklistenpruefungVerlaufEintraege");
        }
    }
}
