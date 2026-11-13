using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Friends;

/// <summary>
/// Строка списка друзей: номер, имя ссылкой на профиль, ближайший ДР с пояснением и число
/// вишей. Раньше это была строка текста <c>1. Имя 05.03 (через 12 дней, 36 лет): 3 виша</c>,
/// теперь — строка таблицы: дата ушла в свою колонку, в «Виши» осталось только число.
/// </summary>
public static class FriendBirthdayLine
{
    public static void AddColumns(TableBuilder table) =>
        table
            .Header("Друг", "Ближайший ДР", "Виши")
            .Align(TableAlign.Left, TableAlign.Left, TableAlign.Right);

    public static void AppendRow(TableBuilder table, FriendBirthday friend)
    {
        table.Row(
            cell => cell.InlineUrl(friend.Friend.FirstName, friend.Friend.BuildFriendLink()),
            cell => AddBirthdayCell(cell, friend),
            cell =>
            {
                // В колонке только число; ссылка на вишлист — на самом числе
                if (friend.WishesCount == 0)
                {
                    cell.Verbatim("0");
                    return;
                }

                cell.InlineUrl(friend.WishesCount.ToString(), friend.Friend.BuildWishlistLink());
            });
    }

    /// <summary>
    /// Дата ближайшего ДР (через tg-time — клиент покажет её в своём формате) и сколько
    /// лет будет: «05.03.2027, 32 года через 113 дней».
    /// </summary>
    public static void AddBirthdayCell(MessageText text, FriendBirthday friend) =>
        AddBirthdayCell(text, friend.NextBirthday, friend.DaysUntil, friend.Age);

    /// <summary>
    /// То же для строк, где есть только дата рождения (список броней: строка — виш,
    /// а ДР принадлежит его владельцу).
    /// </summary>
    public static void AddBirthdayCell(MessageText text, DateOnly? birthday, DateOnly today)
    {
        if (birthday is not { } date)
        {
            AddBirthdayCell(text, null, null, null);
            return;
        }

        var nextBirthday = BirthdayUtils.GetNextBirthday(date, today);

        AddBirthdayCell(
            text,
            nextBirthday,
            BirthdayUtils.GetDaysUntil(nextBirthday, today),
            BirthdayUtils.GetAge(date, nextBirthday));
    }

    private static void AddBirthdayCell(MessageText text, DateOnly? nextBirthday, int? daysUntil, int? age)
    {
        if (nextBirthday is not { } date)
        {
            text.Verbatim("(ДР не указан)");
            return;
        }

        var relativeText = daysUntil == 0
            ? "сегодня"
            : $"через {daysUntil} {PluralUtils.Days(daysUntil.Value)}";

        text
            .Date(date, "d", date.ToString("dd.MM.yyyy"))
            .Verbatim($", {age} {PluralUtils.Years(age.Value)} ")
            .RelativeDate(date, relativeText);
    }

    /// <summary>Текстовая версия для тестов и логов.</summary>
    public static string Describe(FriendBirthday friend) =>
        friend.NextBirthday is { } nextBirthday
            ? $"{nextBirthday:dd.MM.yyyy}, {BirthdayUtils.DescribeUpcoming(friend.Age!.Value, friend.DaysUntil!.Value)}"
            : "(ДР не указан)";
}
