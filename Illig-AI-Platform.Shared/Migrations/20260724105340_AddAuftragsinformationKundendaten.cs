using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAuftragsinformationKundendaten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kundenadresse",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Kundenname",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kundenadresse",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "Kundenname",
                table: "StuecklistenpruefungVerlaufEintraege");
        }
    }
}
