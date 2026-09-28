using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SportsManagementMVC.Data;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260928150000_AddPhoneLoginToAppUsers")]
    public partial class AddPhoneLoginToAppUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AppUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhone",
                table: "AppUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "AppUsers" AS u
                SET
                    "Phone" = p."Phone",
                    "NormalizedPhone" =
                        CASE
                            WHEN regexp_replace(p."Phone", '[^0-9]', '', 'g') ~ '^0[0-9]{9}$'
                                THEN '27' || substring(regexp_replace(p."Phone", '[^0-9]', '', 'g') from 2)
                            WHEN regexp_replace(p."Phone", '[^0-9]', '', 'g') ~ '^27[0-9]{9}$'
                                THEN regexp_replace(p."Phone", '[^0-9]', '', 'g')
                            ELSE NULL
                        END
                FROM "Players" AS p
                WHERE u."PlayerId" = p."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "AppUsers" AS u
                SET
                    "Phone" = c."Phone",
                    "NormalizedPhone" =
                        CASE
                            WHEN regexp_replace(c."Phone", '[^0-9]', '', 'g') ~ '^0[0-9]{9}$'
                                THEN '27' || substring(regexp_replace(c."Phone", '[^0-9]', '', 'g') from 2)
                            WHEN regexp_replace(c."Phone", '[^0-9]', '', 'g') ~ '^27[0-9]{9}$'
                                THEN regexp_replace(c."Phone", '[^0-9]', '', 'g')
                            ELSE NULL
                        END
                FROM "Coaches" AS c
                WHERE u."Role" = 1
                  AND lower(u."Email") = lower(c."Email")
                  AND u."NormalizedPhone" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedPhone",
                table: "AppUsers",
                column: "NormalizedPhone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedPhone",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedPhone",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AppUsers");
        }
    }
}
