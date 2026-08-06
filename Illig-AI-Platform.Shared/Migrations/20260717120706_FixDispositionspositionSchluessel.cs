using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class FixDispositionspositionSchluessel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dispositionspositionen_Einkaufsbeleg_Position",
                table: "Dispositionspositionen");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Einkaufsbeleg",
                table: "Dispositionspositionen",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Schluessel",
                table: "Dispositionspositionen",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositionspositionen_Schluessel",
                table: "Dispositionspositionen",
                column: "Schluessel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dispositionspositionen_Schluessel",
                table: "Dispositionspositionen");

            migrationBuilder.DropColumn(
                name: "Schluessel",
                table: "Dispositionspositionen");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "Dispositionspositionen",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Einkaufsbeleg",
                table: "Dispositionspositionen",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositionspositionen_Einkaufsbeleg_Position",
                table: "Dispositionspositionen",
                columns: new[] { "Einkaufsbeleg", "Position" },
                unique: true);
        }
    }
}
