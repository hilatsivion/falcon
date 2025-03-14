using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class secondmigsssssssssssss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MailTags",
                table: "MailTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailTags",
                table: "MailTags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MailTags_MailReceivedId",
                table: "MailTags",
                column: "MailReceivedId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags",
                column: "AppUserEmail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MailTags",
                table: "MailTags");

            migrationBuilder.DropIndex(
                name: "IX_MailTags_MailReceivedId",
                table: "MailTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MailTags",
                table: "MailTags",
                columns: new[] { "MailReceivedId", "TagName" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags",
                columns: new[] { "AppUserEmail", "TagName" });
        }
    }
}
