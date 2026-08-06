using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddKundenstamm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Auftragsbestaetigungen",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KundeId",
                table: "Angebote",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kunden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kundennummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalisierterName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalisierteAdresse = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AktualisiertAm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunden", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "KundenQuellen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    KundeId = table.Column<int>(type: "int", nullable: false),
                    Quelltyp = table.Column<int>(type: "int", nullable: false),
                    QuellId = table.Column<int>(type: "int", nullable: false),
                    Kundennummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenname = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenadresse = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErfasstAm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundenQuellen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KundenQuellen_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_KundeId",
                table: "StuecklistenpruefungVerlaufEintraege",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Auftragsbestaetigungen_KundeId",
                table: "Auftragsbestaetigungen",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Angebote_KundeId",
                table: "Angebote",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kunden_Kundennummer",
                table: "Kunden",
                column: "Kundennummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kunden_NormalisierterName_NormalisierteAdresse",
                table: "Kunden",
                columns: new[] { "NormalisierterName", "NormalisierteAdresse" });

            migrationBuilder.CreateIndex(
                name: "IX_KundenQuellen_KundeId",
                table: "KundenQuellen",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_KundenQuellen_Quelltyp_QuellId",
                table: "KundenQuellen",
                columns: new[] { "Quelltyp", "QuellId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Angebote_Kunden_KundeId",
                table: "Angebote",
                column: "KundeId",
                principalTable: "Kunden",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Auftragsbestaetigungen_Kunden_KundeId",
                table: "Auftragsbestaetigungen",
                column: "KundeId",
                principalTable: "Kunden",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StuecklistenpruefungVerlaufEintraege_Kunden_KundeId",
                table: "StuecklistenpruefungVerlaufEintraege",
                column: "KundeId",
                principalTable: "Kunden",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Angebote_Kunden_KundeId",
                table: "Angebote");

            migrationBuilder.DropForeignKey(
                name: "FK_Auftragsbestaetigungen_Kunden_KundeId",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropForeignKey(
                name: "FK_StuecklistenpruefungVerlaufEintraege_Kunden_KundeId",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropTable(
                name: "KundenQuellen");

            migrationBuilder.DropTable(
                name: "Kunden");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_KundeId",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropIndex(
                name: "IX_Auftragsbestaetigungen_KundeId",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropIndex(
                name: "IX_Angebote_KundeId",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "KundeId",
                table: "Angebote");
        }
    }
}
