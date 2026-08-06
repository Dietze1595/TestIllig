using System;
using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728110000_AddAngebotFreigegebenVon")]
public partial class AddAngebotFreigegebenVon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "FreigegebenVonUserProfileId",
            table: "Angebote",
            type: "char(36)",
            nullable: true,
            collation: "ascii_general_ci");

        migrationBuilder.CreateIndex(
            name: "IX_Angebote_FreigegebenVonUserProfileId",
            table: "Angebote",
            column: "FreigegebenVonUserProfileId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Angebote_FreigegebenVonUserProfileId",
            table: "Angebote");

        migrationBuilder.DropColumn(
            name: "FreigegebenVonUserProfileId",
            table: "Angebote");
    }
}
