using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class AddFilterFoldersAndRelationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FilterFolders_AppUsers_AppUserId",
                table: "FilterFolders");

            migrationBuilder.DropIndex(
                name: "IX_FilterFolders_AppUserId",
                table: "FilterFolders");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "FilterFolders");

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "FilterFolderTags",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "FilterFolderId",
                table: "FilterFolderTags",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<string>(
                name: "FolderColor",
                table: "FilterFolders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserEmail",
                table: "FilterFolders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FilterFolders_AppUserEmail",
                table: "FilterFolders",
                column: "AppUserEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterFolders_AppUsers_AppUserEmail",
                table: "FilterFolders",
                column: "AppUserEmail",
                principalTable: "AppUsers",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FilterFolders_AppUsers_AppUserEmail",
                table: "FilterFolders");

            migrationBuilder.DropIndex(
                name: "IX_FilterFolders_AppUserEmail",
                table: "FilterFolders");

            migrationBuilder.DropColumn(
                name: "AppUserEmail",
                table: "FilterFolders");

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "FilterFolderTags",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "FilterFolderId",
                table: "FilterFolderTags",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<string>(
                name: "FolderColor",
                table: "FilterFolders",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "FilterFolders",
                type: "nvarchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FilterFolders_AppUserId",
                table: "FilterFolders",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FilterFolders_AppUsers_AppUserId",
                table: "FilterFolders",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
