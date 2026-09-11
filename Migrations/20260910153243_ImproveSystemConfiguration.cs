using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    /// <inheritdoc />
    public partial class ImproveSystemConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptPlayerApplications",
                table: "OrganisationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowEventRegistration",
                table: "OrganisationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCountry",
                table: "OrganisationSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultProvince",
                table: "OrganisationSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultTeamName",
                table: "OrganisationSettings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceMessage",
                table: "OrganisationSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceMode",
                table: "OrganisationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "OrganisationSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialEmail",
                table: "OrganisationSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialPhone",
                table: "OrganisationSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialWebsite",
                table: "OrganisationSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptPlayerApplications",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "AllowEventRegistration",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "DefaultCountry",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "DefaultProvince",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "DefaultTeamName",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "MaintenanceMessage",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "MaintenanceMode",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "OfficialEmail",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "OfficialPhone",
                table: "OrganisationSettings");

            migrationBuilder.DropColumn(
                name: "OfficialWebsite",
                table: "OrganisationSettings");
        }
    }
}
