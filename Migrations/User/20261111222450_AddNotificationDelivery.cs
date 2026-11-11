using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddNotificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationTopicId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifiedSinceLastMessage",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationTopicId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifiedSinceLastMessage",
                table: "Users");
        }
    }
}
