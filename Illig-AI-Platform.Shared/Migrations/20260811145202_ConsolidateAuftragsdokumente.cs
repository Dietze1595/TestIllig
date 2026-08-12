using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateAuftragsdokumente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Neue Spalten anlegen, bevor Daten aus den alten Tabellen übernommen werden.
            migrationBuilder.AddColumn<int>(
                name: "Quelle",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SharePointDriveId",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SharePointItemId",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ETag",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WebUrl",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SharePointErstelltAm",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SharePointGeaendertAm",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnalyseStatus",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalyseFehler",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerarbeitetAm",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeloeschtAm",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "datetime(6)",
                nullable: true);

            // UserProfileId muss vor dem Daten-Insert nullable sein, da SharePoint-Zeilen
            // (Quelle = 1) keinen User haben und die Spalte sonst NOT NULL verletzt.
            migrationBuilder.AlterColumn<Guid>(
                name: "UserProfileId",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            // Bestehende SharePoint-Auftragsdokumente in die konsolidierte Tabelle übernehmen.
            // Quelle = 1 entspricht AuftragsdokumentQuelle.SharePoint.
            // Die Tabelle Auftragsdokumente kann durch einen vorherigen fehlgeschlagenen
            // Migrationsversuch bereits entfernt worden sein; daher nur ausführen, wenn sie existiert.
            migrationBuilder.Sql(@"
                SET @auftragsdokumenteExistiert = (
                    SELECT COUNT(*) FROM information_schema.tables
                    WHERE table_schema = DATABASE() AND table_name = 'Auftragsdokumente');

                SET @sql = IF(@auftragsdokumenteExistiert > 0,
                    'INSERT INTO StuecklistenpruefungVerlaufEintraege
                        (Quelle, Dateiname, Auftragsnummer, Kundennummer, Kundenname, Kundenadresse,
                         Datum, Maschinentyp, ErstelltAm, ErreichterSchritt,
                         SharePointDriveId, SharePointItemId, ETag, WebUrl,
                         SharePointErstelltAm, SharePointGeaendertAm,
                         AnalyseStatus, AnalyseFehler, VerarbeitetAm, GeloeschtAm)
                    SELECT
                        1, Dateiname, Auftragsnummer, Kundennummer, Kundenname, Kundenadresse,
                        Datum, Maschinentyp, SharePointErstelltAm, 2,
                        SharePointDriveId, SharePointItemId, ETag, WebUrl,
                        SharePointErstelltAm, SharePointGeaendertAm,
                        AnalyseStatus, AnalyseFehler, VerarbeitetAm, GeloeschtAm
                    FROM Auftragsdokumente',
                    'DO 0');

                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Zugehörige Merkmale übernehmen; die Zuordnung erfolgt über SharePointDriveId/ItemId,
            // da die neu vergebenen Ids der Zieltabelle nicht mit den alten AuftragsdokumentId übereinstimmen.
            migrationBuilder.Sql(@"
                SET @merkmaleExistieren = (
                    SELECT COUNT(*) FROM information_schema.tables
                    WHERE table_schema = DATABASE() AND table_name = 'AuftragsdokumentMerkmale')
                    * (SELECT COUNT(*) FROM information_schema.tables
                    WHERE table_schema = DATABASE() AND table_name = 'Auftragsdokumente');

                SET @sql = IF(@merkmaleExistieren > 0,
                    'INSERT INTO VerlaufMerkmale
                        (VerlaufEintragId, Kategorie, Position, Merkmalsnummer, Beschreibung)
                    SELECT
                        v.Id, m.Kategorie, m.Position, m.Merkmalsnummer, m.Beschreibung
                    FROM AuftragsdokumentMerkmale m
                    JOIN Auftragsdokumente a ON a.Id = m.AuftragsdokumentId
                    JOIN StuecklistenpruefungVerlaufEintraege v
                        ON v.Quelle = 1
                        AND v.SharePointDriveId = a.SharePointDriveId
                        AND v.SharePointItemId = a.SharePointItemId',
                    'DO 0');

                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql("DROP TABLE IF EXISTS `AuftragsdokumentMerkmale`;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS `Auftragsdokumente`;");

            migrationBuilder.AlterColumn<string>(
                name: "Maschinentyp",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Kundennummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "BlobPfad",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Auftragsnummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_AnalyseStatus_Geloescht~",
                table: "StuecklistenpruefungVerlaufEintraege",
                columns: new[] { "AnalyseStatus", "GeloeschtAm" });

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Auftragsnummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                column: "Auftragsnummer");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Kundennummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                column: "Kundennummer");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Quelle",
                table: "StuecklistenpruefungVerlaufEintraege",
                column: "Quelle");

            migrationBuilder.CreateIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_SharePointDriveId_Share~",
                table: "StuecklistenpruefungVerlaufEintraege",
                columns: new[] { "SharePointDriveId", "SharePointItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_AnalyseStatus_Geloescht~",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Auftragsnummer",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Kundennummer",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_Quelle",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropIndex(
                name: "IX_StuecklistenpruefungVerlaufEintraege_SharePointDriveId_Share~",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "AnalyseFehler",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "AnalyseStatus",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "ETag",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "GeloeschtAm",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "Quelle",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SharePointDriveId",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SharePointErstelltAm",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SharePointGeaendertAm",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SharePointItemId",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "VerarbeitetAm",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "WebUrl",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserProfileId",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Maschinentyp",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Kundennummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "StuecklistenpruefungVerlaufEintraege",
                keyColumn: "BlobPfad",
                keyValue: null,
                column: "BlobPfad",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "BlobPfad",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Auftragsnummer",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Auftragsdokumente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AnalyseFehler = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnalyseStatus = table.Column<int>(type: "int", nullable: false),
                    Auftragsnummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dateiname = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Datum = table.Column<DateOnly>(type: "date", nullable: true),
                    ETag = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeloeschtAm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Kundenadresse = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundenname = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kundennummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Maschinentyp = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SharePointDriveId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SharePointErstelltAm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SharePointGeaendertAm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SharePointItemId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VerarbeitetAm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WebUrl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auftragsdokumente", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuftragsdokumentMerkmale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuftragsdokumentId = table.Column<int>(type: "int", nullable: false),
                    Beschreibung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kategorie = table.Column<int>(type: "int", nullable: false),
                    Merkmalsnummer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Position = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
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
        }
    }
}
