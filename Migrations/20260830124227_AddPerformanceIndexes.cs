using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status_Date",
                table: "Reports",
                columns: new[] { "Status", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Status",
                table: "Players",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status_Date",
                table: "Matches",
                columns: new[] { "Status", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_Date",
                table: "Events",
                columns: new[] { "Status", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_Date",
                table: "Attendances",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_IsPinned_Date",
                table: "Announcements",
                columns: new[] { "IsPinned", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_Status_Date",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Players_Status",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Status_Date",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Events_Status_Date",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_Date",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_IsPinned_Date",
                table: "Announcements");
        }
    }
}
