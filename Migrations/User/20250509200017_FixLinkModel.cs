using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class FixLinkModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_WishDrafts_WishDraftId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_WishDrafts_WishDraftModelWishDraftId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_Wishes_WishModelWishId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Users_ClaimerId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalId",
                table: "WishDrafts");

            migrationBuilder.DropIndex(
                name: "IX_Links_WishDraftModelWishDraftId",
                table: "Links");

            migrationBuilder.DropIndex(
                name: "IX_Links_WishModelWishId",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "WishDraftModelWishDraftId",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "WishModelWishId",
                table: "Links");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_WishDrafts_WishDraftId",
                table: "Links",
                column: "WishDraftId",
                principalTable: "WishDrafts",
                principalColumn: "WishDraftId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links",
                column: "WishId",
                principalTable: "Wishes",
                principalColumn: "WishId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Users_ClaimerId",
                table: "WishDrafts",
                column: "ClaimerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalId",
                table: "WishDrafts",
                column: "OriginalId",
                principalTable: "Wishes",
                principalColumn: "WishId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_WishDrafts_WishDraftId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Users_ClaimerId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalId",
                table: "WishDrafts");

            migrationBuilder.AddColumn<int>(
                name: "WishDraftModelWishDraftId",
                table: "Links",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WishModelWishId",
                table: "Links",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishDraftModelWishDraftId",
                table: "Links",
                column: "WishDraftModelWishDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishModelWishId",
                table: "Links",
                column: "WishModelWishId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_WishDrafts_WishDraftId",
                table: "Links",
                column: "WishDraftId",
                principalTable: "WishDrafts",
                principalColumn: "WishDraftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_WishDrafts_WishDraftModelWishDraftId",
                table: "Links",
                column: "WishDraftModelWishDraftId",
                principalTable: "WishDrafts",
                principalColumn: "WishDraftId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links",
                column: "WishId",
                principalTable: "Wishes",
                principalColumn: "WishId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Wishes_WishModelWishId",
                table: "Links",
                column: "WishModelWishId",
                principalTable: "Wishes",
                principalColumn: "WishId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Users_ClaimerId",
                table: "WishDrafts",
                column: "ClaimerId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalId",
                table: "WishDrafts",
                column: "OriginalId",
                principalTable: "Wishes",
                principalColumn: "WishId");
        }
    }
}
