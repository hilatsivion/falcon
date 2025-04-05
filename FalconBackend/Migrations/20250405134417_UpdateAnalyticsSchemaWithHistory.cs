using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class UpdateAnalyticsSchemaWithHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailsReceivedLastWeek",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmailsSentLastWeek",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDailyReset",
                table: "Analytics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWeeklyReset",
                table: "Analytics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ReadEmailsLastWeek",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpamEmailsLastWeek",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "TimeSpentLastWeek",
                table: "Analytics",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TimeSpentYesterday",
                table: "Analytics",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailsReceivedLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "EmailsSentLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "LastDailyReset",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "LastWeeklyReset",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "ReadEmailsLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "SpamEmailsLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "TimeSpentLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "TimeSpentYesterday",
                table: "Analytics");
        }
    }
}
