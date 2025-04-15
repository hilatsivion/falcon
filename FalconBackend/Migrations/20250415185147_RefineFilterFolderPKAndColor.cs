using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class RefineFilterFolderPKAndColor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilterFolders",
                columns: table => new
                {
                    FilterFolderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FolderColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterFolders", x => x.FilterFolderId);
                    table.ForeignKey(
                        name: "FK_FilterFolders_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FilterFolderTags",
                columns: table => new
                {
                    FilterFolderId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterFolderTags", x => new { x.FilterFolderId, x.TagId });
                    table.ForeignKey(
                        name: "FK_FilterFolderTags_FilterFolders_FilterFolderId",
                        column: x => x.FilterFolderId,
                        principalTable: "FilterFolders",
                        principalColumn: "FilterFolderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FilterFolderTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilterFolders_AppUserId",
                table: "FilterFolders",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FilterFolderTags_TagId",
                table: "FilterFolderTags",
                column: "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilterFolderTags");

            migrationBuilder.DropTable(
                name: "FilterFolders");
        }
    }
}
