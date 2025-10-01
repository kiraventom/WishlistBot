using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddProfileType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "ProfileModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "ProfileModel");
        }
    }
}
