using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximalstuecklistenPositionen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaschinentypStuecklisten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaschinentypSchluessel = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kopfmaterial = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportiertAm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaschinentypStuecklisten", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaximalstuecklistenPositionen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaschinentypStuecklisteId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Reihenfolge = table.Column<int>(type: "int", nullable: false),
                    Artikelnummer = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bezeichnung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Menge = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Einheit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bedingung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaximalstuecklistenPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaximalstuecklistenPositionen_MaschinentypStuecklisten_Masch~",
                        column: x => x.MaschinentypStuecklisteId,
                        principalTable: "MaschinentypStuecklisten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MaschinentypStuecklisten_MaschinentypSchluessel",
                table: "MaschinentypStuecklisten",
                column: "MaschinentypSchluessel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaximalstuecklistenPositionen_MaschinentypStuecklisteId",
                table: "MaximalstuecklistenPositionen",
                column: "MaschinentypStuecklisteId");

            migrationBuilder.CreateIndex(
                name: "IX_MaximalstuecklistenPositionen_ParentId",
                table: "MaximalstuecklistenPositionen",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaximalstuecklistenPositionen");

            migrationBuilder.DropTable(
                name: "MaschinentypStuecklisten");
        }
    }
}
