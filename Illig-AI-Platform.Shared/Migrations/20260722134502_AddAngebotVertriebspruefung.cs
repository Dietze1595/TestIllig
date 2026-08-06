using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAngebotVertriebspruefung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GueltigBis",
                table: "Angebote",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GueltigkeitsdatumKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Liefertermin",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LieferterminKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Verkaeufer",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VerkaeuferKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZahlungsbedingungCode",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Zahlungsplan",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZahlungsplanKommentar",
                table: "Angebote",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GueltigBis",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "GueltigkeitsdatumKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "Liefertermin",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "LieferterminKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "Verkaeufer",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "VerkaeuferKommentar",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "ZahlungsbedingungCode",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "Zahlungsplan",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "ZahlungsplanKommentar",
                table: "Angebote");
        }
    }
}
