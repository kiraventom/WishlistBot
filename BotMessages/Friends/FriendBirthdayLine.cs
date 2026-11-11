using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Friends;

/// <summary>
/// Строка списка друзей: <c>1. Имя 05.03 (через 12 дней, 36 лет): 3 виша</c>.
/// «Имя + дата» ведут в профиль друга, «N виша» — в его вишлист.
/// Если вишей нет: <c>1. Имя (ДР не указан): вишлист пуст</c>.
/// </summary>
public static class FriendBirthdayLine
{
    public static void Append(MessageText text, int number, FriendBirthday friend)
    {
        text.Bold($"{number}. ");

        // имя и дата — ссылка на профиль
        var profileText = friend.NextBirthday is { } nextBirthday
            ? $"{friend.Friend.FirstName} {nextBirthday.ToString("dd.MM")}"
            : friend.Friend.FirstName;

        text.InlineUrl(profileText, friend.Friend.BuildFriendLink());
        text.Verbatim($" {DescribeWhen(friend)}: ");

        if (friend.WishesCount == 0)
        {
            text.Verbatim("вишлист пуст");
            return;
        }

        text.InlineUrl(
            $"{friend.WishesCount} {PluralUtils.Wishes(friend.WishesCount)}",
            friend.Friend.BuildWishlistLink());
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
