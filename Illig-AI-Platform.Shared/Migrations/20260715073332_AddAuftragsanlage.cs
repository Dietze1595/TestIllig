using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAuftragsanlage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Angebote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Angebotsnummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Kundenname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenadresse = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Zahlungsbedingungen = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dateiname = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlobPfad = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HochgeladenAm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Angebote", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AngebotPositionen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AngebotId = table.Column<int>(type: "int", nullable: false),
                    Beschreibung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Menge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Einzelpreis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gesamtbetrag = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AngebotPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AngebotPositionen_Angebote_AngebotId",
                        column: x => x.AngebotId,
                        principalTable: "Angebote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Auftragsbestaetigungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AngebotId = table.Column<int>(type: "int", nullable: false),
                    Nummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenadresse = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Zahlungsbedingungen = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dateiname = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlobPfad = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HochgeladenAm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auftragsbestaetigungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auftragsbestaetigungen_Angebote_AngebotId",
                        column: x => x.AngebotId,
                        principalTable: "Angebote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuftragsbestaetigungPositionen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuftragsbestaetigungId = table.Column<int>(type: "int", nullable: false),
                    Beschreibung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Menge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Einzelpreis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gesamtbetrag = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuftragsbestaetigungPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuftragsbestaetigungPositionen_Auftragsbestaetigungen_Auftra~",
                        column: x => x.AuftragsbestaetigungId,
                        principalTable: "Auftragsbestaetigungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Angebote_Angebotsnummer_Version",
                table: "Angebote",
                columns: new[] { "Angebotsnummer", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AngebotPositionen_AngebotId",
                table: "AngebotPositionen",
                column: "AngebotId");

            migrationBuilder.CreateIndex(
                name: "IX_Auftragsbestaetigungen_AngebotId",
                table: "Auftragsbestaetigungen",
                column: "AngebotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuftragsbestaetigungPositionen_AuftragsbestaetigungId",
                table: "AuftragsbestaetigungPositionen",
                column: "AuftragsbestaetigungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AngebotPositionen");

            migrationBuilder.DropTable(
                name: "AuftragsbestaetigungPositionen");

            migrationBuilder.DropTable(
                name: "Auftragsbestaetigungen");

            migrationBuilder.DropTable(
                name: "Angebote");
        }
    }
}
