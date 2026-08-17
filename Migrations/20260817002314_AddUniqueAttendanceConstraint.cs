using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueAttendanceConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_PlayerId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_PlayerId_EventId",
                table: "Attendances",
                columns: new[] { "PlayerId", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_PlayerId_EventId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_PlayerId",
                table: "Attendances",
                column: "PlayerId");
        }
    }
}
