using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportsManagementMVC.Data;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260917130000_AddPlayerProfileDetails")]
    public partial class AddPlayerProfileDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerProfileDetails",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "date", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfileDetails", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerProfileDetails_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""PlayerProfileDetails""
                    (""PlayerId"", ""JoinedDate"", ""EmergencyContactName"", ""EmergencyContactPhone"")
                SELECT
                    p.""Id"",
                    COALESCE(
                        (
                            SELECT MIN(pra.""SubmittedAtUtc""::date)
                            FROM ""PlayerRegistrationApplications"" pra
                            WHERE LOWER(pra.""Email"") = LOWER(p.""Email"")
                        ),
                        (
                            SELECT MIN(er.""RegisteredAtUtc""::date)
                            FROM ""EventRegistrations"" er
                            WHERE er.""PlayerId"" = p.""Id""
                        ),
                        (
                            SELECT MIN(a.""Date"")
                            FROM ""Attendances"" a
                            WHERE a.""PlayerId"" = p.""Id""
                        ),
                        CURRENT_DATE
                    ),
                    COALESCE(
                        (
                            SELECT pra.""EmergencyContactName""
                            FROM ""PlayerRegistrationApplications"" pra
                            WHERE LOWER(pra.""Email"") = LOWER(p.""Email"")
                              AND pra.""EmergencyContactName"" IS NOT NULL
                            ORDER BY pra.""SubmittedAtUtc"" DESC
                            LIMIT 1
                        ),
                        ''
                    ),
                    COALESCE(
                        (
                            SELECT pra.""EmergencyContactPhone""
                            FROM ""PlayerRegistrationApplications"" pra
                            WHERE LOWER(pra.""Email"") = LOWER(p.""Email"")
                              AND pra.""EmergencyContactPhone"" IS NOT NULL
                            ORDER BY pra.""SubmittedAtUtc"" DESC
                            LIMIT 1
                        ),
                        ''
                    )
                FROM ""Players"" p;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerProfileDetails");
        }
    }
}
