using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddBirthdayNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // По умолчанию напоминания включены — как и остальные уведомления у новых пользователей
            migrationBuilder.AddColumn<bool>(
                name: "BirthdayNotifications",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ScheduledNotifications",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LastRunDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotifications", x => x.Name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledNotifications");

            migrationBuilder.DropColumn(
                name: "BirthdayNotifications",
                table: "Settings");
        }
    }
}
