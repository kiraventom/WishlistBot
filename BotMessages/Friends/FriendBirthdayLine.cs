using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Friends;

/// <summary>
/// Строка списка друзей: <c>1. Имя 05.03 (через 12 дней, 36 лет): 3 виша</c>.
/// Для друга без даты рождения: <c>1. Имя (ДР не указан): 3 виша</c>.
/// </summary>
public static class FriendBirthdayLine
{
    public static void Append(MessageText text, int number, FriendBirthday friend)
    {
        text.Bold($"{number}. ");
        text.InlineUrl(friend.Friend.FirstName, friend.Friend.BuildWishlistLink());
        text.Verbatim($" {Describe(friend)}: {friend.WishesCount} {PluralUtils.Wishes(friend.WishesCount)}");
    }

    public static string Describe(FriendBirthday friend)
    {
        if (friend.NextBirthday is not { } nextBirthday)
            return "(ДР не указан)";

        var date = nextBirthday.ToString("dd.MM");
        var daysUntil = friend.DaysUntil!.Value;
        var age = friend.Age!.Value;

        var daysText = daysUntil == 0
            ? "сегодня"
            : $"через {daysUntil} {PluralUtils.Days(daysUntil)}";

        return $"{date} ({daysText}, {age} {PluralUtils.Years(age)})";
    }
}
