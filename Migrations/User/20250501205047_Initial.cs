using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Broadcasts",
                columns: table => new
                {
                    BroadcastId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeSent = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Broadcasts", x => x.BroadcastId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    SubscribeId = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    BotState = table.Column<int>(type: "INTEGER", nullable: false),
                    LastQueryId = table.Column<string>(type: "TEXT", nullable: true),
                    LastBotMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    QueryParams = table.Column<string>(type: "TEXT", nullable: true),
                    AllowedQueries = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Extra = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedBroadcasts",
                columns: table => new
                {
                    ReceivedBroadcastId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceiverId = table.Column<int>(type: "INTEGER", nullable: false),
                    BroadcastId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedBroadcasts", x => x.ReceivedBroadcastId);
                    table.ForeignKey(
                        name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                        column: x => x.BroadcastId,
                        principalTable: "Broadcasts",
                        principalColumn: "BroadcastId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedBroadcasts_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SendNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceiveNotifications = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingsId);
                    table.ForeignKey(
                        name: "FK_Settings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubscriberId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wishes",
                columns: table => new
                {
                    WishId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    PriceRange = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishes", x => x.WishId);
                    table.ForeignKey(
                        name: "FK_Wishes_Users_ClaimerId",
                        column: x => x.ClaimerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Wishes_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WishDrafts",
                columns: table => new
                {
                    WishDraftId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalId = table.Column<int>(type: "INTEGER", nullable: true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    PriceRange = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishDrafts", x => x.WishDraftId);
                    table.ForeignKey(
                        name: "FK_WishDrafts_Users_ClaimerId",
                        column: x => x.ClaimerId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_WishDrafts_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishDrafts_Wishes_OriginalId",
                        column: x => x.OriginalId,
                        principalTable: "Wishes",
                        principalColumn: "WishId");
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WishId = table.Column<int>(type: "INTEGER", nullable: true),
                    WishDraftId = table.Column<int>(type: "INTEGER", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    WishDraftModelWishDraftId = table.Column<int>(type: "INTEGER", nullable: true),
                    WishModelWishId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_Links_WishDrafts_WishDraftId",
                        column: x => x.WishDraftId,
                        principalTable: "WishDrafts",
                        principalColumn: "WishDraftId");
                    table.ForeignKey(
                        name: "FK_Links_WishDrafts_WishDraftModelWishDraftId",
                        column: x => x.WishDraftModelWishDraftId,
                        principalTable: "WishDrafts",
                        principalColumn: "WishDraftId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Links_Wishes_WishId",
                        column: x => x.WishId,
                        principalTable: "Wishes",
                        principalColumn: "WishId");
                    table.ForeignKey(
                        name: "FK_Links_Wishes_WishModelWishId",
                        column: x => x.WishModelWishId,
                        principalTable: "Wishes",
                        principalColumn: "WishId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishDraftId",
                table: "Links",
                column: "WishDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishDraftModelWishDraftId",
                table: "Links",
                column: "WishDraftModelWishDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishId",
                table: "Links",
                column: "WishId");

            migrationBuilder.CreateIndex(
                name: "IX_Links_WishModelWishId",
                table: "Links",
                column: "WishModelWishId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SourceId",
                table: "Notifications",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_BroadcastId",
                table: "ReceivedBroadcasts",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_ReceiverId",
                table: "ReceivedBroadcasts",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId",
                table: "Settings",
                column: "UserId",
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
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_ClaimerId",
                table: "WishDrafts",
                column: "ClaimerId");

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_OriginalId",
                table: "WishDrafts",
                column: "OriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_OwnerId",
                table: "WishDrafts",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_ClaimerId",
                table: "Wishes",
                column: "ClaimerId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_Order",
                table: "Wishes",
                column: "Order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_OwnerId",
                table: "Wishes",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReceivedBroadcasts");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "WishDrafts");

            migrationBuilder.DropTable(
                name: "Broadcasts");

            migrationBuilder.DropTable(
                name: "Wishes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
