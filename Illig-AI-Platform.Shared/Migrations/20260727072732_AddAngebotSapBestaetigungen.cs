using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddAngebotSapBestaetigungen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SapFuehrendBestaetigt",
                table: "Angebote",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SapSparteBestaetigt",
                table: "Angebote",
                type: "tinyint(1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SapFuehrendBestaetigt",
                table: "Angebote");

            migrationBuilder.DropColumn(
                name: "SapSparteBestaetigt",
                table: "Angebote");
        }
    }
}
