using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportsManagementMVC.Data;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915105500_AddEventReminderDispatches")]
    public partial class AddEventReminderDispatches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventReminderDispatches",
                columns: table => new
                {
                    EventId = table.Column<int>(
                        type: "integer",
                        nullable: false),
                    ScheduledForSast = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false),
                    ClaimedAtSast = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false),
                    SentAtSast = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_EventReminderDispatches",
                        x => new
                        {
                            x.EventId,
                            x.ScheduledForSast
                        });

                    table.ForeignKey(
                        name: "FK_EventReminderDispatches_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReminderDispatches");
        }
    }
}
