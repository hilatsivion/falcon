using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class secondmigssss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags");

            migrationBuilder.RenameColumn(
                name: "Discriminator",
                table: "Mails",
                newName: "MailType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags",
                columns: new[] { "AppUserEmail", "TagName" });

            migrationBuilder.CreateTable(
                name: "MailTags",
                columns: table => new
                {
                    MailReceivedId = table.Column<int>(type: "int", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailTags", x => new { x.MailReceivedId, x.TagName });
                    table.ForeignKey(
                        name: "FK_MailTags_Mails_MailReceivedId",
                        column: x => x.MailReceivedId,
                        principalTable: "Mails",
                        principalColumn: "MailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MailTags_Tags_TagName",
                        column: x => x.TagName,
                        principalTable: "Tags",
                        principalColumn: "TagName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailTags_TagName",
                table: "MailTags",
                column: "TagName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags");

            migrationBuilder.RenameColumn(
                name: "MailType",
                table: "Mails",
                newName: "Discriminator");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteTags",
                table: "FavoriteTags",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTags_AppUserEmail",
                table: "FavoriteTags",
                column: "AppUserEmail");
        }
    }
}
