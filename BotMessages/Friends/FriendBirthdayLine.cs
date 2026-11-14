using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Friends;

/// <summary>
/// Строка списка друзей: имя ссылкой на профиль, ближайший ДР и число вишей.
/// Раньше это была строка текста <c>1. Имя 05.03 (через 12 дней, 36 лет): 3 виша</c>,
/// теперь — строка таблицы: дата ушла в свою колонку, в «Виши» осталось только число.
/// В «ДР» — «10 марта, 32 года»: год рождения ничего не решает (он всегда этот или
/// следующий), а относительное время («через 12 дней») — задача напоминаний, не списка.
/// </summary>
public static class FriendBirthdayLine
{
    /// <summary>Подпись колонки ДР. Короткая — таблица иначе расползается на узком экране.</summary>
    public const string BirthdayColumn = "ДР";

    /// <summary>Что пишем в колонке ДР, когда дата не задана.</summary>
    public const string NoBirthday = "не указан";

    public static void AddColumns(TableBuilder table) =>
        table
            .Header("Друг", BirthdayColumn, "Виши")
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
    /// Дата ближайшего ДР и сколько лет будет: «10 марта, 32 года». Возраста нет, если
    /// человек скрыл год рождения: «10 марта».
    /// </summary>
    public static void AddBirthdayCell(MessageText text, FriendBirthday friend) =>
        AddBirthdayCell(text, friend.NextBirthday, friend.Age);

    /// <summary>
    /// То же для строк, где есть только дата рождения (список броней: строка — виш,
    /// а ДР принадлежит его владельцу).
    /// </summary>
    public static void AddBirthdayCell(MessageText text, Birthday? birthday, DateOnly today)
    {
        if (birthday is not { } value)
        {
            AddBirthdayCell(text, null, null);
            return;
        }

        var nextBirthday = BirthdayUtils.GetNextBirthday(value, today);

        AddBirthdayCell(text, nextBirthday, BirthdayUtils.GetAge(value, nextBirthday));
    }

    /// <summary>
    /// Дата — своим текстом через <c>tg-time</c> с пустым форматом: в списке видно
    /// «10 марта» без года, а нажатие всё равно показывает дату в формате и поясе
    /// пользователя (<see cref="MessageText.DateAsText"/>).
    /// </summary>
    private static void AddBirthdayCell(MessageText text, DateOnly? nextBirthday, int? age)
    {
        if (nextBirthday is not { } date)
        {
            text.Verbatim(NoBirthday);
            return;
        }

        text.DateAsText(date, RussianDate.DayMonth(date));

        if (age is { } years)
            text.Verbatim($", {AgeText(years)}");
    }

    private static string AgeText(int age) => $"{age} {PluralUtils.Years(age)}";

    /// <summary>Текстовая версия для тестов и логов.</summary>
    public static string Describe(FriendBirthday friend)
    {
        if (friend.NextBirthday is not { } nextBirthday)
            return NoBirthday;

        var dateText = RussianDate.DayMonth(nextBirthday);

        return friend.Age is { } age ? $"{dateText}, {AgeText(age)}" : dateText;
    }
}
