using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations
{
    /// <inheritdoc />
    public partial class EmojiThumbnails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomEmojiFileId",
                table: "StoredMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomEmojiId",
                table: "StoredMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmojiOwnerUserId",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThumbAttempts",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomEmojiFileId",
                table: "StoredMedia");

            migrationBuilder.DropColumn(
                name: "CustomEmojiId",
                table: "StoredMedia");

            migrationBuilder.DropColumn(
                name: "EmojiOwnerUserId",
                table: "StoredMedia");

            migrationBuilder.DropColumn(
                name: "ThumbAttempts",
                table: "StoredMedia");
        }
    }
}
