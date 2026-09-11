using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SportsManagementMVC.Migrations
{
    /// <inheritdoc />
    public partial class ImproveBackupManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "BackupRecords",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutomatic",
                table: "BackupRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "BackupRecords",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "BackupRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackupSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutomaticEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    HourSast = table.Column<int>(type: "integer", nullable: false),
                    WeeklyDay = table.Column<int>(type: "integer", nullable: false),
                    RetentionCount = table.Column<int>(type: "integer", nullable: false),
                    LastAutomaticRunAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupSettings");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "BackupRecords");

            migrationBuilder.DropColumn(
                name: "IsAutomatic",
                table: "BackupRecords");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "BackupRecords");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "BackupRecords");
        }
    }
}
