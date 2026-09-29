using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsManagementMVC.Migrations
{
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
