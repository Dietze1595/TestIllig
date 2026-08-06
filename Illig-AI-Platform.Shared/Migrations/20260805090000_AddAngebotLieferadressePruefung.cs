using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260805090000_AddAngebotLieferadressePruefung")]
public partial class AddAngebotLieferadressePruefung : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Lieferadresse",
            table: "Angebote",
            type: "longtext",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LieferadresseKommentar",
            table: "Angebote",
            type: "longtext",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Lieferadresse", table: "Angebote");
        migrationBuilder.DropColumn(name: "LieferadresseKommentar", table: "Angebote");
    }
}
