using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AddListPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListPositionsModel",
                columns: table => new
                {
                    ListPositionModelId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewerId = table.Column<int>(type: "INTEGER", nullable: false),
                    WishPage = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriberPage = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriptionPage = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimPage = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminBroadcastPage = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminUserPage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListPositionsModel", x => x.ListPositionModelId);
                    table.ForeignKey(
                        name: "FK_ListPositionsModel_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListPositionsModel_Users_ViewerId",
                        column: x => x.ViewerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_TargetId",
                table: "ListPositionsModel",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_ViewerId",
                table: "ListPositionsModel",
                column: "ViewerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListPositionsModel");
        }
    }
}
