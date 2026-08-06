using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddVerlaufMerkmal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerkmaleJson",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.DropColumn(
                name: "SonderoptionenJson",
                table: "StuecklistenpruefungVerlaufEintraege");

            migrationBuilder.CreateTable(
                name: "VerlaufMerkmale",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VerlaufEintragId = table.Column<int>(type: "int", nullable: false),
                    Kategorie = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Merkmalsnummer = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Beschreibung = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerlaufMerkmale", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerlaufMerkmale_StuecklistenpruefungVerlaufEintraege_Verlauf~",
                        column: x => x.VerlaufEintragId,
                        principalTable: "StuecklistenpruefungVerlaufEintraege",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VerlaufMerkmale_Kategorie_Merkmalsnummer",
                table: "VerlaufMerkmale",
                columns: new[] { "Kategorie", "Merkmalsnummer" });

            migrationBuilder.CreateIndex(
                name: "IX_VerlaufMerkmale_VerlaufEintragId",
                table: "VerlaufMerkmale",
                column: "VerlaufEintragId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerlaufMerkmale");

            migrationBuilder.AddColumn<string>(
                name: "MerkmaleJson",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SonderoptionenJson",
                table: "StuecklistenpruefungVerlaufEintraege",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
