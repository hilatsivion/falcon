using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class AddSystemInterestTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "TagName", "TagType" },
                values: new object[,]
                {
                    { -9, "health", "SystemTag" },
                    { -8, "personal", "SystemTag" },
                    { -7, "family & friends", "SystemTag" },
                    { -6, "finance", "SystemTag" },
                    { -5, "discounts", "SystemTag" },
                    { -4, "news", "SystemTag" },
                    { -3, "social network", "SystemTag" },
                    { -2, "school", "SystemTag" },
                    { -1, "work", "SystemTag" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -10);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -9);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -8);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -7);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -6);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -5);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -4);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -3);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: -1);
        }
    }
}
