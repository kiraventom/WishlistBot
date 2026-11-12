using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations
{
    /// <inheritdoc />
    public partial class MediaBlobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredMedia_FileId_MessageId",
                table: "StoredMedia");

            // Картинки переезжают из канала-хранилища в базу: file_id становится только
            // кэшем, а сам файл лежит блобом. У старых записей ключ = их file_id,
            // иначе ссылки из вишей перестали бы находиться.
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "StoredMedia",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TelegramFileId",
                table: "StoredMedia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "StoredMedia",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LegacyMessageId",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportAttempts",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "StoredMedia",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Переносим легаси-данные до того, как старые колонки исчезнут
            migrationBuilder.Sql("""
                UPDATE "StoredMedia"
                SET "Key" = "FileId",
                    "TelegramFileId" = "FileId",
                    "LegacyMessageId" = "MessageId",
                    "CreatedAt" = datetime('now');
                """);

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "StoredMedia");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "StoredMedia");

            migrationBuilder.CreateIndex(
                name: "IX_StoredMedia_Key",
                table: "StoredMedia",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredMedia_TelegramFileId",
                table: "StoredMedia",
                column: "TelegramFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredMedia_Key",
                table: "StoredMedia");

            migrationBuilder.DropIndex(
                name: "IX_StoredMedia_TelegramFileId",
                table: "StoredMedia");

            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "StoredMedia",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MessageId",
                table: "StoredMedia",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "StoredMedia"
                SET "FileId" = "Key",
                    "MessageId" = COALESCE("LegacyMessageId", 0);
                """);

            migrationBuilder.DropColumn(name: "Key", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "TelegramFileId", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "Data", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "Width", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "Height", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "SizeBytes", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "LegacyMessageId", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "ImportAttempts", table: "StoredMedia");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "StoredMedia");

            migrationBuilder.CreateIndex(
                name: "IX_StoredMedia_FileId_MessageId",
                table: "StoredMedia",
                columns: new[] { "FileId", "MessageId" },
                unique: true);
        }
    }
}
