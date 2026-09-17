using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    /// <summary>
    /// Synchronizes the EF Core model snapshot with schema changes that
    /// are already represented by earlier migrations.
    ///
    /// No database operations are required.
    /// </summary>
    public partial class SyncModelSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
            // PlayerProfilePhotos, PlayerProfileDetails and
            // AnnouncementReadReceipts are created by their own migrations.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
        }
    }
}