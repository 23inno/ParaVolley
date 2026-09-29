using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportsManagementMVC.Data;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260929115000_AddPendingApplicationEmailUniqueness")]
    public partial class AddPendingApplicationEmailUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlayerRegistrationApplications_PendingEmail",
                table: "PlayerRegistrationApplications",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL AND \"Status\" = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerRegistrationApplications_PendingEmail",
                table: "PlayerRegistrationApplications");
        }
    }
}
