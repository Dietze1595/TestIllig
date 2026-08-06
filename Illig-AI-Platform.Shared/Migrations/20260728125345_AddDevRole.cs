using Illig_AI_Platform.Shared.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260728125345_AddDevRole")]
public partial class AddDevRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            $"INSERT INTO `Roles` (`Id`, `Name`) VALUES (7, '{AppRoles.Dev}');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM `Roles` WHERE `Id` = 7;");
    }
}
