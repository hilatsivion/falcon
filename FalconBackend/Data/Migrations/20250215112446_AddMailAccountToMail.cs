using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Data.Migrations
{
    public partial class AddMailAccountToMail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                table: "Mails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MailAccountId",
                table: "Mails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Mails_MailAccountId",
                table: "Mails",
                column: "MailAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mails_MailAccounts_MailAccountId",
                table: "Mails",
                column: "MailAccountId",
                principalTable: "MailAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mails_MailAccounts_MailAccountId",
                table: "Mails");

            migrationBuilder.DropIndex(
                name: "IX_Mails_MailAccountId",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "IsSent",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "MailAccountId",
                table: "Mails");
        }
    }
}
