using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class RenameMediaKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Wishes",
                newName: "MediaKey");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "WishDrafts",
                newName: "MediaKey");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Broadcasts",
                newName: "MediaKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MediaKey",
                table: "Wishes",
                newName: "FileId");

            migrationBuilder.RenameColumn(
                name: "MediaKey",
                table: "WishDrafts",
                newName: "FileId");

            migrationBuilder.RenameColumn(
                name: "MediaKey",
                table: "Broadcasts",
                newName: "FileId");
        }
    }
}
