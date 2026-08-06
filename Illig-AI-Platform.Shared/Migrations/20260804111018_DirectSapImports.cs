using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class DirectSapImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SapStagingEintraege");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenPositionen_Auftragsnummer",
                table: "StuecklistenPositionen");

            migrationBuilder.AddColumn<string>(
                name: "Auftragsposition",
                table: "StuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BomTyp",
                table: "StuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "GueltigAm",
                table: "StuecklistenPositionen",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "NodeId",
                table: "StuecklistenPositionen",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ParentNodeId",
                table: "StuecklistenPositionen",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RootNodeId",
                table: "StuecklistenPositionen",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SapPosition",
                table: "StuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Typ",
                table: "StuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Dokumentnummer",
                table: "MaximalstuecklistenPositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Dokumenttyp",
                table: "MaximalstuecklistenPositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Dokumentversion",
                table: "MaximalstuecklistenPositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Gesamtmenge",
                table: "MaximalstuecklistenPositionen",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SapNodeId",
                table: "MaximalstuecklistenPositionen",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SapPosition",
                table: "MaximalstuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Typ",
                table: "MaximalstuecklistenPositionen",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Beschreibung",
                table: "MaschinentypStuecklisten",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BomTyp",
                table: "MaschinentypStuecklisten",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "GueltigAm",
                table: "MaschinentypStuecklisten",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Stuecklistenalternative",
                table: "MaschinentypStuecklisten",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Stuecklistenverwendung",
                table: "MaschinentypStuecklisten",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Werk",
                table: "MaschinentypStuecklisten",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KontaktTyp",
                table: "LieferantEmailAdressen",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EinkaeuferEmail",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EinkaeufergruppenName",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Einteilungsnummer",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LabNr",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MaterialgruppenCode",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MaterialgruppenName",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VerantwortlichePerson",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE StuecklistenPositionen
                SET BomTyp = 'LEGACY',
                    NodeId = CONCAT('legacy-', Id),
                    RootNodeId = CONCAT('legacy-', Id),
                    Typ = 'LEGACY',
                    GueltigAm = DATE(ImportiertAm)
                WHERE NodeId = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE MaschinentypStuecklisten
                SET BomTyp = 'LEGACY',
                    Beschreibung = MaschinentypSchluessel,
                    GueltigAm = DATE(ImportiertAm)
                WHERE BomTyp = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE MaximalstuecklistenPositionen
                SET SapNodeId = CONCAT('legacy-', Id),
                    Typ = 'LEGACY'
                WHERE SapNodeId = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenPositionen_Auftragsnummer_Auftragsposition",
                table: "StuecklistenPositionen",
                columns: new[] { "Auftragsnummer", "Auftragsposition" });

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenPositionen_NodeId",
                table: "StuecklistenPositionen",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaximalstuecklistenPositionen_MaschinentypStuecklisteId_SapN~",
                table: "MaximalstuecklistenPositionen",
                columns: new[] { "MaschinentypStuecklisteId", "SapNodeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StuecklistenPositionen_Auftragsnummer_Auftragsposition",
                table: "StuecklistenPositionen");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenPositionen_NodeId",
                table: "StuecklistenPositionen");

            migrationBuilder.DropIndex(
                name: "IX_MaximalstuecklistenPositionen_MaschinentypStuecklisteId_SapN~",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Auftragsposition",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "BomTyp",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "GueltigAm",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "ParentNodeId",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "RootNodeId",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "SapPosition",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Typ",
                table: "StuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Dokumentnummer",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Dokumenttyp",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Dokumentversion",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Gesamtmenge",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "SapNodeId",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "SapPosition",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Typ",
                table: "MaximalstuecklistenPositionen");

            migrationBuilder.DropColumn(
                name: "Beschreibung",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "BomTyp",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "GueltigAm",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "Stuecklistenalternative",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "Stuecklistenverwendung",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "Werk",
                table: "MaschinentypStuecklisten");

            migrationBuilder.DropColumn(
                name: "KontaktTyp",
                table: "LieferantEmailAdressen");

            migrationBuilder.DropColumn(
                name: "EinkaeuferEmail",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "EinkaeufergruppenName",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "Einteilungsnummer",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "LabNr",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "MaterialgruppenCode",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "MaterialgruppenName",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "VerantwortlichePerson",
                table: "Dispositionspositionen");

            migrationBuilder.CreateTable(
                name: "SapStagingEintraege",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpfangenAm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UseCase = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SapStagingEintraege", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenPositionen_Auftragsnummer",
                table: "StuecklistenPositionen",
                column: "Auftragsnummer");

            migrationBuilder.CreateIndex(
                name: "IX_SapStagingEintraege_UseCase_EmpfangenAm",
                table: "SapStagingEintraege",
                columns: new[] { "UseCase", "EmpfangenAm" });
        }
    }
}
