using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FalconBackend.Migrations
{
    public partial class EmptyMigrationToSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty migration - database already has required OAuth columns
            // This migration just syncs the EF model snapshot with the current database state
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty migration - no changes to revert
        }
    }
}
