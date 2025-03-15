using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class AddAnalyticsTrackingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Analytics_AppUserEmail",
                table: "Analytics");

            migrationBuilder.AddColumn<float>(
                name: "AvgEmailsPerDay",
                table: "Analytics",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AvgEmailsPerWeek",
                table: "Analytics",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStreak",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActiveToday",
                table: "Analytics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastResetDate",
                table: "Analytics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LongestStreak",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDaysTracked",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "TotalTimeSpent",
                table: "Analytics",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgEmailsPerDay",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "AvgEmailsPerWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "CurrentStreak",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "IsActiveToday",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "LastResetDate",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "LongestStreak",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "TotalDaysTracked",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "TotalTimeSpent",
                table: "Analytics");

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_AppUserEmail",
                table: "Analytics",
                column: "AppUserEmail");
        }
    }
}
