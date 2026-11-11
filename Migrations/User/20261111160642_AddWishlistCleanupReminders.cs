using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddWishlistCleanupReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Напоминания включены по умолчанию — как и остальные уведомления
            migrationBuilder.AddColumn<bool>(
                name: "WishlistCleanupNotifications",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "WishlistCleanupStates",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReminderDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextReminderDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistCleanupStates", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_WishlistCleanupStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WishlistCleanupStates");

            migrationBuilder.DropColumn(
                name: "WishlistCleanupNotifications",
                table: "Settings");
        }
    }
}
