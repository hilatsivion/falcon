using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class AddPreviousPeriodAnalyticss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedEmailsLastWeek",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedEmailsWeekly",
                table: "Analytics",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedEmailsLastWeek",
                table: "Analytics");

            migrationBuilder.DropColumn(
                name: "DeletedEmailsWeekly",
                table: "Analytics");
        }
    }
}
