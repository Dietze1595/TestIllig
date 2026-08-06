using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations;

/// <inheritdoc />
public partial class AddSharePointAuftragsinformationen : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Die ursprünglich generierte Migration enthielt versehentlich das komplette
        // Datenbankschema und versuchte deshalb bestehende Tabellen erneut anzulegen.
        // Auftragsdokumente wird per IF NOT EXISTS angelegt, weil ein abgebrochener Lauf
        // unter MySQL diese erste Tabelle bereits persistiert haben kann, ohne die Migration
        // in __EFMigrationsHistory einzutragen.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `Auftragsdokumente` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `SharePointDriveId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `SharePointItemId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `ETag` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
                `Dateiname` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                `WebUrl` varchar(2048) CHARACTER SET utf8mb4 NOT NULL,
                `SharePointErstelltAm` datetime(6) NOT NULL,
                `SharePointGeaendertAm` datetime(6) NOT NULL,
                `Auftragsnummer` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                `Kundennummer` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                `Kundenname` varchar(500) CHARACTER SET utf8mb4 NULL,
                `Kundenadresse` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `Datum` date NULL,
                `Maschinentyp` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                `AnalyseStatus` int NOT NULL,
                `AnalyseFehler` longtext CHARACTER SET utf8mb4 NULL,
                `VerarbeitetAm` datetime(6) NULL,
                `GeloeschtAm` datetime(6) NULL,
                CONSTRAINT `PK_Auftragsdokumente` PRIMARY KEY (`Id`)
            ) CHARACTER SET=utf8mb4;
            """);

        migrationBuilder.CreateTable(
            name: "SharePointSynchronisationsstaende",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Quelle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DriveId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                OrdnerItemId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DeltaLink = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                LetzterVersuchAm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                LetzterErfolgAm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                LetzterFehler = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SharePointSynchronisationsstaende", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "AuftragsdokumentMerkmale",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                AuftragsdokumentId = table.Column<int>(type: "int", nullable: false),
                Kategorie = table.Column<int>(type: "int", nullable: false),
                Position = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Merkmalsnummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Beschreibung = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuftragsdokumentMerkmale", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuftragsdokumentMerkmale_Auftragsdokumente_AuftragsdokumentId",
                    column: x => x.AuftragsdokumentId,
                    principalTable: "Auftragsdokumente",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Auftragsdokumente_AnalyseStatus_GeloeschtAm",
            table: "Auftragsdokumente",
            columns: new[] { "AnalyseStatus", "GeloeschtAm" });

        migrationBuilder.CreateIndex(
            name: "IX_Auftragsdokumente_Auftragsnummer",
            table: "Auftragsdokumente",
            column: "Auftragsnummer");

        migrationBuilder.CreateIndex(
            name: "IX_Auftragsdokumente_Kundennummer",
            table: "Auftragsdokumente",
            column: "Kundennummer");

        migrationBuilder.CreateIndex(
            name: "IX_Auftragsdokumente_SharePointDriveId_SharePointItemId",
            table: "Auftragsdokumente",
            columns: new[] { "SharePointDriveId", "SharePointItemId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuftragsdokumentMerkmale_AuftragsdokumentId",
            table: "AuftragsdokumentMerkmale",
            column: "AuftragsdokumentId");

        migrationBuilder.CreateIndex(
            name: "IX_AuftragsdokumentMerkmale_Kategorie_Merkmalsnummer",
            table: "AuftragsdokumentMerkmale",
            columns: new[] { "Kategorie", "Merkmalsnummer" });

        migrationBuilder.CreateIndex(
            name: "IX_SharePointSynchronisationsstaende_Quelle",
            table: "SharePointSynchronisationsstaende",
            column: "Quelle",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuftragsdokumentMerkmale");
        migrationBuilder.DropTable(name: "SharePointSynchronisationsstaende");
        migrationBuilder.DropTable(name: "Auftragsdokumente");
    }
}
