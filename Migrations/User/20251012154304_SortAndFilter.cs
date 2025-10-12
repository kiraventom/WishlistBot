using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class SortAndFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WishViewSettingsModel",
                columns: table => new
                {
                    WishViewSettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descending = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortProperty = table.Column<int>(type: "INTEGER", nullable: false),
                    OnlyUnclaimed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishViewSettingsModel", x => x.WishViewSettingsId);
                    table.ForeignKey(
                        name: "FK_WishViewSettingsModel_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishViewSettingsModel_Users_ViewerId",
                        column: x => x.ViewerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WishViewSettingsModel_TargetId",
                table: "WishViewSettingsModel",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_WishViewSettingsModel_ViewerId",
                table: "WishViewSettingsModel",
                column: "ViewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WishViewSettingsModel");
        }
    }
}
