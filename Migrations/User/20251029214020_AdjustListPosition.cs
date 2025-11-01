using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class AdjustListPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_Users_TargetId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_Users_ViewerId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_TargetId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_ViewerId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "AdminBroadcastPage",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "AdminUserPage",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "ClaimPage",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "SubscriberPage",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "SubscriptionPage",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "ViewerId",
                table: "ListPositionsModel");

            migrationBuilder.RenameColumn(
                name: "WishPage",
                table: "ListPositionsModel",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ListPositionModelId",
                table: "ListPositionsModel",
                newName: "ListPositionsId");

            migrationBuilder.AddColumn<int>(
                name: "AdminBroadcastPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminUserPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClaimPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriberPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WishPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ListPositionModel",
                columns: table => new
                {
                    ListPositionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Page = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListPositionModel", x => x.ListPositionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_AdminBroadcastPageListPositionId",
                table: "ListPositionsModel",
                column: "AdminBroadcastPageListPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_AdminUserPageListPositionId",
                table: "ListPositionsModel",
                column: "AdminUserPageListPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_ClaimPageListPositionId",
                table: "ListPositionsModel",
                column: "ClaimPageListPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_SubscriberPageListPositionId",
                table: "ListPositionsModel",
                column: "SubscriberPageListPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel",
                column: "SubscriptionPageListPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_UserId",
                table: "ListPositionsModel",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_WishPageListPositionId",
                table: "ListPositionsModel",
                column: "WishPageListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_AdminBroadcastPageListPositionId",
                table: "ListPositionsModel",
                column: "AdminBroadcastPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_AdminUserPageListPositionId",
                table: "ListPositionsModel",
                column: "AdminUserPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_ClaimPageListPositionId",
                table: "ListPositionsModel",
                column: "ClaimPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriberPageListPositionId",
                table: "ListPositionsModel",
                column: "SubscriberPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel",
                column: "SubscriptionPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_WishPageListPositionId",
                table: "ListPositionsModel",
                column: "WishPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_Users_UserId",
                table: "ListPositionsModel",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_AdminBroadcastPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_AdminUserPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_ClaimPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriberPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_Users_UserId",
                table: "ListPositionsModel");

            migrationBuilder.DropTable(
                name: "ListPositionModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_AdminBroadcastPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_AdminUserPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_ClaimPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_SubscriberPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_UserId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "AdminBroadcastPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "AdminUserPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "ClaimPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "SubscriberPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "SubscriptionPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ListPositionsModel",
                newName: "WishPage");

            migrationBuilder.RenameColumn(
                name: "ListPositionsId",
                table: "ListPositionsModel",
                newName: "ListPositionModelId");

            migrationBuilder.AddColumn<int>(
                name: "AdminBroadcastPage",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdminUserPage",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClaimPage",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriberPage",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPage",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewerId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_TargetId",
                table: "ListPositionsModel",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_ViewerId",
                table: "ListPositionsModel",
                column: "ViewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_Users_TargetId",
                table: "ListPositionsModel",
                column: "TargetId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_Users_ViewerId",
                table: "ListPositionsModel",
                column: "ViewerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
