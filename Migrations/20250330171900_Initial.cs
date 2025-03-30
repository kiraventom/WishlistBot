using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations
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
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeSent = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    BotState = table.Column<int>(type: "INTEGER", nullable: false),
                    LastQueryId = table.Column<string>(type: "TEXT", nullable: true),
                    LastBotMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    QueryParams = table.Column<string>(type: "TEXT", nullable: true),
                    AllowedQueries = table.Column<string>(type: "TEXT", nullable: true),
                    SubscribeId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedBroadcasts",
                columns: table => new
                {
                    ReceivedBroadcastId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceiverUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    BroadcastId = table.Column<int>(type: "INTEGER", nullable: true),
                    MessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedBroadcasts", x => x.ReceivedBroadcastId);
                    table.ForeignKey(
                        name: "FK_ReceivedBroadcasts_Broadcasts_BroadcastId",
                        column: x => x.BroadcastId,
                        principalTable: "Broadcasts",
                        principalColumn: "BroadcastId");
                    table.ForeignKey(
                        name: "FK_ReceivedBroadcasts_Users_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
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
                    SubscriberUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_SubscriberUserId",
                        column: x => x.SubscriberUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Wishes",
                columns: table => new
                {
                    WishId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClaimerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    PriceRange = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishes", x => x.WishId);
                    table.ForeignKey(
                        name: "FK_Wishes_Users_ClaimerUserId",
                        column: x => x.ClaimerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Wishes_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "WishDrafts",
                columns: table => new
                {
                    WishDraftId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalWishId = table.Column<int>(type: "INTEGER", nullable: true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    PriceRange = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishDrafts", x => x.WishDraftId);
                    table.ForeignKey(
                        name: "FK_WishDrafts_Users_ClaimerUserId",
                        column: x => x.ClaimerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_WishDrafts_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishDrafts_Wishes_OriginalWishId",
                        column: x => x.OriginalWishId,
                        principalTable: "Wishes",
                        principalColumn: "WishId");
                });

            migrationBuilder.CreateTable(
                name: "LinkModel",
                columns: table => new
                {
                    LinkId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WishId = table.Column<int>(type: "INTEGER", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    WishDraftModelWishDraftId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkModel", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_LinkModel_WishDrafts_WishDraftModelWishDraftId",
                        column: x => x.WishDraftModelWishDraftId,
                        principalTable: "WishDrafts",
                        principalColumn: "WishDraftId");
                    table.ForeignKey(
                        name: "FK_LinkModel_Wishes_WishId",
                        column: x => x.WishId,
                        principalTable: "Wishes",
                        principalColumn: "WishId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkModel_WishDraftModelWishDraftId",
                table: "LinkModel",
                column: "WishDraftModelWishDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkModel_WishId",
                table: "LinkModel",
                column: "WishId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_BroadcastId",
                table: "ReceivedBroadcasts",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedBroadcasts_ReceiverUserId",
                table: "ReceivedBroadcasts",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId",
                table: "Settings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberUserId",
                table: "Subscriptions",
                column: "SubscriberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TargetUserId",
                table: "Subscriptions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_ClaimerUserId",
                table: "WishDrafts",
                column: "ClaimerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_OriginalWishId",
                table: "WishDrafts",
                column: "OriginalWishId");

            migrationBuilder.CreateIndex(
                name: "IX_WishDrafts_OwnerId",
                table: "WishDrafts",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_ClaimerUserId",
                table: "Wishes",
                column: "ClaimerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishes_OwnerUserId",
                table: "Wishes",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkModel");

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
