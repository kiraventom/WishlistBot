using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WishlistBot.Migrations.User
{
    /// <summary>
    /// Дата рождения переехала из одной колонки <c>Birthday</c> (TEXT «yyyy-MM-dd»)
    /// в три: день, месяц и год. Год может быть скрыт — Telegram отдаёт дату рождения
    /// без года, если человек так настроил приватность, и хранить «10 марта» было негде.
    ///
    /// Колонки добавляются и заполняются **до** удаления старой: иначе данные теряются.
    /// </summary>
    public partial class BirthdayWithoutYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthdayDay",
                table: "ProfileModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthdayMonth",
                table: "ProfileModel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthdayYear",
                table: "ProfileModel",
                type: "INTEGER",
                nullable: true);

            // DateOnly в SQLite — это TEXT «yyyy-MM-dd», его и разбираем
            migrationBuilder.Sql(
                """
                UPDATE ProfileModel
                SET BirthdayDay = CAST(strftime('%d', Birthday) AS INTEGER),
                    BirthdayMonth = CAST(strftime('%m', Birthday) AS INTEGER),
                    BirthdayYear = CAST(strftime('%Y', Birthday) AS INTEGER)
                WHERE Birthday IS NOT NULL
                  AND strftime('%Y-%m-%d', Birthday) IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "ProfileModel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Birthday",
                table: "ProfileModel",
                type: "TEXT",
                nullable: true);

            // Обратно возвращаются только полные даты: у «10 марта» года нет, и придумывать
            // его нельзя — такие даты при откате теряются (это осознанная потеря).
            migrationBuilder.Sql(
                """
                UPDATE ProfileModel
                SET Birthday = printf('%04d-%02d-%02d', BirthdayYear, BirthdayMonth, BirthdayDay)
                WHERE BirthdayDay IS NOT NULL
                  AND BirthdayMonth IS NOT NULL
                  AND BirthdayYear IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "BirthdayDay",
                table: "ProfileModel");

            migrationBuilder.DropColumn(
                name: "BirthdayMonth",
                table: "ProfileModel");

            migrationBuilder.DropColumn(
                name: "BirthdayYear",
                table: "ProfileModel");
        }
    }
}
