using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Illig_AI_Platform.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddBestaetigungUserProfileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserProfileId",
                table: "Auftragsbestaetigungen",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Auftragsbestaetigungen_UserProfileId",
                table: "Auftragsbestaetigungen",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Auftragsbestaetigungen_UserProfileId",
                table: "Auftragsbestaetigungen");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "Auftragsbestaetigungen");
        }
    }
}
