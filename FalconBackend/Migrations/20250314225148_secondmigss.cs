using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class secondmigss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_TagName_MailAccountId",
                table: "FavoriteTags");

            migrationBuilder.AlterColumn<string>(
                name: "MailAccountId",
                table: "FavoriteTags",
                type: "nvarchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AppUserEmail",
                table: "FavoriteTags",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags",
                column: "AppUserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_TagName_AppUserEmail",
                table: "FavoriteTags",
                columns: new[] { "TagName", "AppUserEmail" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteTags_AppUsers_AppUserEmail",
                table: "FavoriteTags",
                column: "AppUserEmail",
                principalTable: "AppUsers",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags",
                column: "MailAccountId",
                principalTable: "MailAccounts",
                principalColumn: "MailAccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteTags_AppUsers_AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_TagName_AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.DropColumn(
                name: "AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiry",
                table: "AppUsers");

            migrationBuilder.AlterColumn<string>(
                name: "MailAccountId",
                table: "FavoriteTags",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_TagName_MailAccountId",
                table: "FavoriteTags",
                columns: new[] { "TagName", "MailAccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteTags_MailAccounts_MailAccountId",
                table: "FavoriteTags",
                column: "MailAccountId",
                principalTable: "MailAccounts",
                principalColumn: "MailAccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
