using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddWishlistFillReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WishlistFillNotifications",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                // Напоминания включены по умолчанию — как и остальные уведомления
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "WishlistFillStates",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReminderDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistFillStates", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_WishlistFillStates_Users_UserId",
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
                name: "WishlistFillStates");

            migrationBuilder.DropColumn(
                name: "WishlistFillNotifications",
                table: "Settings");
        }
    }
}
