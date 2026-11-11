namespace WishlistBot.Model.User;

/// <summary>Друг с посчитанным ближайшим днём рождения.</summary>
public sealed record FriendBirthday(
    UserModel Friend,
    DateOnly? NextBirthday,
    int? DaysUntil,
    int? Age,
    int WishesCount);

public static class BirthdayUtils
{
    /// <summary>
    /// Ближайший день рождения: в этом году, либо в следующем, если в этом уже прошёл.
    /// 29 февраля в невисокосный год считается 28-м.
    /// </summary>
    public static DateOnly GetNextBirthday(DateOnly birthday, DateOnly today)
    {
        var next = InYear(birthday, today.Year);
        return next < today ? InYear(birthday, today.Year + 1) : next;
    }

    public static int GetDaysUntil(DateOnly nextBirthday, DateOnly today) =>
        nextBirthday.DayNumber - today.DayNumber;

    /// <summary>Сколько лет исполнится в ближайший день рождения.</summary>
    public static int GetAge(DateOnly birthday, DateOnly nextBirthday) =>
        nextBirthday.Year - birthday.Year;

    /// <summary>
    /// Друзья, отсортированные по ближайшему ДР: сначала те, у кого дата указана
    /// (по возрастанию), затем те, у кого её нет (по имени).
    /// </summary>
    public static List<FriendBirthday> SortByUpcomingBirthday(IEnumerable<UserModel> friends, DateOnly today)
    {
        var result = new List<FriendBirthday>();

        foreach (var friend in friends)
        {
            var wishesCount = friend.Wishes.Count;

            if (friend.Profile?.Birthday is not { } birthday)
            {
                result.Add(new FriendBirthday(friend, null, null, null, wishesCount));
                continue;
            }

            var nextBirthday = GetNextBirthday(birthday, today);
            result.Add(new FriendBirthday(
                friend,
                nextBirthday,
                GetDaysUntil(nextBirthday, today),
                GetAge(birthday, nextBirthday),
                wishesCount));
        }

        return result
            .OrderBy(friend => friend.NextBirthday ?? DateOnly.MaxValue)
            .ThenByDescending(friend => friend.WishesCount)
            .ThenBy(friend => friend.Friend.FirstName)
            .ToList();
    }

    private static DateOnly InYear(DateOnly birthday, int year)
    {
        var day = birthday.Day;

        if (birthday.Month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            day = 28;

        return new DateOnly(year, birthday.Month, day);
    }
}
