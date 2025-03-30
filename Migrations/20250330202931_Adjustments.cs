using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations
{
    /// <inheritdoc />
    public partial class Adjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkModel_WishDrafts_WishDraftModelWishDraftId",
                table: "LinkModel");

            migrationBuilder.DropForeignKey(
                name: "FK_LinkModel_Wishes_WishId",
                table: "LinkModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedBroadcasts_Users_ReceiverUserId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_SubscriberUserId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_TargetUserId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Users_ClaimerUserId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalWishId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishes_Users_ClaimerUserId",
                table: "Wishes");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishes_Users_OwnerUserId",
                table: "Wishes");

            migrationBuilder.DropIndex(
                name: "IX_Wishes_ClaimerUserId",
                table: "Wishes");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_SubscriberUserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TargetUserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedBroadcasts_ReceiverUserId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LinkModel",
                table: "LinkModel");

            migrationBuilder.DropColumn(
                name: "ClaimerUserId",
                table: "Wishes");

            migrationBuilder.DropColumn(
                name: "SubscriberUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "ReceivedBroadcasts");

            migrationBuilder.RenameTable(
                name: "LinkModel",
                newName: "Links");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Wishes",
                newName: "ClaimerId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishes_OwnerUserId",
                table: "Wishes",
                newName: "IX_Wishes_ClaimerId");

            migrationBuilder.RenameColumn(
                name: "OriginalWishId",
                table: "WishDrafts",
                newName: "OriginalId");

            migrationBuilder.RenameColumn(
                name: "ClaimerUserId",
                table: "WishDrafts",
                newName: "ClaimerId");

            migrationBuilder.RenameIndex(
                name: "IX_WishDrafts_OriginalWishId",
                table: "WishDrafts",
                newName: "IX_WishDrafts_OriginalId");

            migrationBuilder.RenameIndex(
                name: "IX_WishDrafts_ClaimerUserId",
                table: "WishDrafts",
                newName: "IX_WishDrafts_ClaimerId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkModel_WishId",
                table: "Links",
                newName: "IX_Links_WishId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkModel_WishDraftModelWishDraftId",
                table: "Links",
                newName: "IX_Links_WishDraftModelWishDraftId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Wishes",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Wishes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WishDrafts",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubscribeId",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LastBotMessageId",
                table: "Users",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriberId",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetId",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "BroadcastId",
                table: "ReceivedBroadcasts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiverId",
                table: "ReceivedBroadcasts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Broadcasts",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateTimeSent",
                table: "Broadcasts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "WishId",
                table: "Links",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Links",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Links",
                table: "Links",
                column: "LinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_OwnerId",
                table: "Wishes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberId_TargetId",
                table: "Subscriptions",
                columns: new[] { "SubscriberId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TargetId",
                table: "Subscriptions",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_ReceiverId",
                table: "ReceivedBroadcasts",
                column: "ReceiverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_WishDrafts_WishDraftModelWishDraftId",
                table: "Links",
                column: "WishDraftModelWishDraftId",
                principalTable: "WishDrafts",
                principalColumn: "WishDraftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links",
                column: "WishId",
                principalTable: "Wishes",
                principalColumn: "WishId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                table: "ReceivedBroadcasts",
                column: "BroadcastId",
                principalTable: "Broadcasts",
                principalColumn: "BroadcastId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedBroadcasts_Users_ReceiverId",
                table: "ReceivedBroadcasts",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_SubscriberId",
                table: "Subscriptions",
                column: "SubscriberId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_TargetId",
                table: "Subscriptions",
                column: "TargetId",
                principalTable: "Users",
                principalColumn: "UserId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Wishes_Users_ClaimerId",
                table: "Wishes",
                column: "ClaimerId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishes_Users_OwnerId",
                table: "Wishes",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Links_WishDrafts_WishDraftModelWishDraftId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_Links_Wishes_WishId",
                table: "Links");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedBroadcasts_Users_ReceiverId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_SubscriberId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_TargetId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Users_ClaimerId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalId",
                table: "WishDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishes_Users_ClaimerId",
                table: "Wishes");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishes_Users_OwnerId",
                table: "Wishes");

            migrationBuilder.DropIndex(
                name: "IX_Wishes_OwnerId",
                table: "Wishes");

            migrationBuilder.DropIndex(
                name: "IX_Users_TelegramId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_SubscriberId_TargetId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TargetId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedBroadcasts_ReceiverId",
                table: "ReceivedBroadcasts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Links",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Wishes");

            migrationBuilder.DropColumn(
                name: "SubscriberId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ReceiverId",
                table: "ReceivedBroadcasts");

            migrationBuilder.RenameTable(
                name: "Links",
                newName: "LinkModel");

            migrationBuilder.RenameColumn(
                name: "ClaimerId",
                table: "Wishes",
                newName: "OwnerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishes_ClaimerId",
                table: "Wishes",
                newName: "IX_Wishes_OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "OriginalId",
                table: "WishDrafts",
                newName: "OriginalWishId");

            migrationBuilder.RenameColumn(
                name: "ClaimerId",
                table: "WishDrafts",
                newName: "ClaimerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_WishDrafts_OriginalId",
                table: "WishDrafts",
                newName: "IX_WishDrafts_OriginalWishId");

            migrationBuilder.RenameIndex(
                name: "IX_WishDrafts_ClaimerId",
                table: "WishDrafts",
                newName: "IX_WishDrafts_ClaimerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Links_WishId",
                table: "LinkModel",
                newName: "IX_LinkModel_WishId");

            migrationBuilder.RenameIndex(
                name: "IX_Links_WishDraftModelWishDraftId",
                table: "LinkModel",
                newName: "IX_LinkModel_WishDraftModelWishDraftId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Wishes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "ClaimerUserId",
                table: "Wishes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WishDrafts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SubscribeId",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "LastBotMessageId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "SubscriberUserId",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetUserId",
                table: "Subscriptions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BroadcastId",
                table: "ReceivedBroadcasts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ReceiverUserId",
                table: "ReceivedBroadcasts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Broadcasts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateTimeSent",
                table: "Broadcasts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WishId",
                table: "LinkModel",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "LinkModel",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LinkModel",
                table: "LinkModel",
                column: "LinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_ClaimerUserId",
                table: "Wishes",
                column: "ClaimerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberUserId",
                table: "Subscriptions",
                column: "SubscriberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TargetUserId",
                table: "Subscriptions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_ReceiverUserId",
                table: "ReceivedBroadcasts",
                column: "ReceiverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkModel_WishDrafts_WishDraftModelWishDraftId",
                table: "LinkModel",
                column: "WishDraftModelWishDraftId",
                principalTable: "WishDrafts",
                principalColumn: "WishDraftId");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkModel_Wishes_WishId",
                table: "LinkModel",
                column: "WishId",
                principalTable: "Wishes",
                principalColumn: "WishId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                table: "ReceivedBroadcasts",
                column: "BroadcastId",
                principalTable: "Broadcasts",
                principalColumn: "BroadcastId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedBroadcasts_Users_ReceiverUserId",
                table: "ReceivedBroadcasts",
                column: "ReceiverUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_SubscriberUserId",
                table: "Subscriptions",
                column: "SubscriberUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_TargetUserId",
                table: "Subscriptions",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Users_ClaimerUserId",
                table: "WishDrafts",
                column: "ClaimerUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishDrafts_Wishes_OriginalWishId",
                table: "WishDrafts",
                column: "OriginalWishId",
                principalTable: "Wishes",
                principalColumn: "WishId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishes_Users_ClaimerUserId",
                table: "Wishes",
                column: "ClaimerUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishes_Users_OwnerUserId",
                table: "Wishes",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
