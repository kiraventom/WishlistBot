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
            .Header("#", "Друг", "Ближайший ДР", "Виши")
            .Align(TableAlign.Right, TableAlign.Left, TableAlign.Left, TableAlign.Right);

    public static void AppendRow(TableBuilder table, int number, FriendBirthday friend)
    {
        table.Row(
            cell => cell.Verbatim(number.ToString()),
            cell => cell.InlineUrl(friend.Friend.FirstName, friend.Friend.BuildFriendLink()),
            cell => cell.Verbatim(Describe(friend)),
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

    /// <summary>Дата ближайшего ДР и пояснение в скобках.</summary>
    public static string Describe(FriendBirthday friend) =>
        friend.NextBirthday is { } nextBirthday
            ? $"{nextBirthday.ToString("dd.MM")} {DescribeWhen(friend)}"
            : "(ДР не указан)";

    private static string DescribeWhen(FriendBirthday friend)
    {
        if (friend.NextBirthday is null)
            return "(ДР не указан)";

        var daysUntil = friend.DaysUntil!.Value;
        var age = friend.Age!.Value;

        var daysText = daysUntil == 0
            ? "сегодня"
            : $"через {daysUntil} {PluralUtils.Days(daysUntil)}";

        return $"({daysText}, {age} {PluralUtils.Years(age)})";
    }
}
