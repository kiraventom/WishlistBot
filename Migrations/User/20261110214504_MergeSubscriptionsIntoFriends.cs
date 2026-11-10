using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <summary>
    /// Подписки и подписчики схлопываются в одну симметричную сущность «друзья».
    /// Данные переносятся по принципу «лучше добавить, чем убрать»: каждая
    /// существующая направленная связь даёт обе строки — и «A — друг B», и
    /// «B — друг A», поэтому никто не теряет доступ к вишлисту, на который был
    /// подписан, а вторая сторона просто получает такую же связь.
    /// </summary>
    public partial class MergeSubscriptionsIntoFriends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriberPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.CreateTable(
                name: "Friends",
                columns: table => new
                {
                    FriendshipId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FriendId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friends", x => x.FriendshipId);
                    table.ForeignKey(
                        name: "FK_Friends_Users_FriendId",
                        column: x => x.FriendId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friends_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Friends_FriendId",
                table: "Friends",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_UserId_FriendId",
                table: "Friends",
                columns: new[] { "UserId", "FriendId" },
                unique: true);

            // Каждая подписка превращается в дружбу в обе стороны: UNION убирает
            // дубли для пар, которые уже были подписаны друг на друга.
            migrationBuilder.Sql("""
                INSERT INTO "Friends" ("UserId", "FriendId")
                SELECT "SubscriberId", "TargetId" FROM "Subscriptions" WHERE "SubscriberId" <> "TargetId"
                UNION
                SELECT "TargetId", "SubscriberId" FROM "Subscriptions" WHERE "SubscriberId" <> "TargetId";
                """);

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ListPositionsModel_SubscriberPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.DropColumn(
                name: "SubscriberPageListPositionId",
                table: "ListPositionsModel");

            // Страница списка подписок переиспользуется как страница списка друзей,
            // поэтому у тех, кто листал подписки, сохранится позиция.
            migrationBuilder.RenameColumn(
                name: "SubscriptionPageListPositionId",
                table: "ListPositionsModel",
                newName: "FriendPageListPositionId");

            migrationBuilder.RenameIndex(
                name: "IX_ListPositionsModel_SubscriptionPageListPositionId",
                table: "ListPositionsModel",
                newName: "IX_ListPositionsModel_FriendPageListPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_FriendPageListPositionId",
                table: "ListPositionsModel",
                column: "FriendPageListPositionId",
                principalTable: "ListPositionModel",
                principalColumn: "ListPositionId");

            // Строки ListPositionModel, на которые ссылалась удалённая колонка
            // подписчиков, остались бы мусором в базе. EF пересобирает таблицу
            // ListPositionsModel в конце миграции, поэтому сначала обнуляем ссылки
            // (колонка всё равно удаляется), и только потом чистим строки —
            // иначе DELETE упрётся в FK.
            migrationBuilder.Sql("""
                UPDATE "ListPositionsModel" SET "SubscriberPageListPositionId" = NULL;
                DELETE FROM "ListPositionModel"
                WHERE "ListPositionId" NOT IN (
                    SELECT "WishPageListPositionId" FROM "ListPositionsModel" WHERE "WishPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "FriendPageListPositionId" FROM "ListPositionsModel" WHERE "FriendPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "ClaimPageListPositionId" FROM "ListPositionsModel" WHERE "ClaimPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "AdminBroadcastPageListPositionId" FROM "ListPositionsModel" WHERE "AdminBroadcastPageListPositionId" IS NOT NULL
                    UNION
                    SELECT "AdminUserPageListPositionId" FROM "ListPositionsModel" WHERE "AdminUserPageListPositionId" IS NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListPositionsModel_ListPositionModel_FriendPageListPositionId",
                table: "ListPositionsModel");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubscriberId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberId_TargetId",
                table: "Subscriptions",
                columns: new[] { "SubscriberId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TargetId",
                table: "Subscriptions",
                column: "TargetId");

            // Вниз данные переносятся как есть: каждая зеркальная пара станет
            // двусторонней подпиской, исходное направление уже не восстановить
            migrationBuilder.Sql("""
                INSERT INTO "Subscriptions" ("SubscriberId", "TargetId")
                SELECT "UserId", "FriendId" FROM "Friends" WHERE "UserId" <> "FriendId";
                """);

            migrationBuilder.DropTable(
                name: "Friends");

            migrationBuilder.RenameColumn(
                name: "FriendPageListPositionId",
                table: "ListPositionsModel",
                newName: "SubscriptionPageListPositionId");

            migrationBuilder.RenameIndex(
                name: "IX_ListPositionsModel_FriendPageListPositionId",
                table: "ListPositionsModel",
                newName: "IX_ListPositionsModel_SubscriptionPageListPositionId");

            migrationBuilder.AddColumn<int>(
                name: "SubscriberPageListPositionId",
                table: "ListPositionsModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListPositionsModel_SubscriberPageListPositionId",
                table: "ListPositionsModel",
                column: "SubscriberPageListPositionId");

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
        }
    }
}
