using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <inheritdoc />
    public partial class MoveWishPageToViewSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Открытая страница вишлиста переехала из общей позиции списков в настройки
            // просмотра пары «зритель → чей вишлист». Старую колонку удаляем, а строки
            // ListPositionModel, на которые она ссылалась, остались бы мусором.
            // EF пересобирает таблицу ListPositionsModel только в конце миграции,
            // поэтому сначала обнуляем ссылки (колонка всё равно удаляется), и только
            // потом чистим осиротевшие строки — иначе DELETE упрётся в FK.
            migrationBuilder.Sql("""
                UPDATE "ListPositionsModel" SET "WishPageListPositionId" = NULL;
                DELETE FROM "ListPositionModel"
                WHERE "ListPositionId" NOT IN (
                    SELECT "FriendPageListPositionId" FROM "ListPositionsModel" WHERE "FriendPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "ClaimPageListPositionId" FROM "ListPositionsModel" WHERE "ClaimPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "AdminBroadcastPageListPositionId" FROM "ListPositionsModel" WHERE "AdminBroadcastPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "AdminUserPageListPositionId" FROM "ListPositionsModel" WHERE "AdminUserPageListPositionId" IS NOT NULL
                );
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "WishPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.AddColumn<int>(
                name: "Page",
                table: "WishViewSettingsModel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Page",
                table: "WishViewSettingsModel");

            migrationBuilder.AddColumn<int>(
                name: "WishPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_WishPageListPositionId",
                table: "ListPositionsModel",
                column: "WishPageListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_WishPageListPositionId",
                table: "ListPositionsModel",
                column: "WishPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");
        }
    }
}
