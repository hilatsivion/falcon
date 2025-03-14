using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class secondmigsss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_MailAccountId",
                table: "FavoriteTags");

            migrationBuilder.DropColumn(
                name: "MailAccountId",
                table: "FavoriteTags");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailAccountId",
                table: "FavoriteTags",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_MailAccountId",
                table: "FavoriteTags",
                column: "MailAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags",
                column: "MailAccountId",
                principalTable: "MailAccounts",
                principalColumn: "MailAccountId");
        }
    }
}
