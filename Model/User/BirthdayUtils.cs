using WishlistBot.Text;

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
    /// Дата рождения из трёх колонок профиля. null — даты нет; мусор (день 31 в апреле,
    /// год из будущего) тоже читается как «нет даты»: лучше пустая колонка в списке,
    /// чем падение на <see cref="DateOnly"/> или «-3 года» в возрасте.
    /// </summary>
    public static Birthday? ToBirthday(int? day, int? month, int? year)
    {
        if (day is not { } d || month is not { } m)
            return null;

        if (!Birthday.IsValidDayMonth(d, m))
            return null;

        if (year is { } y)
        {
            if (!Birthday.IsValidYear(y))
                return null;

            // 29.02 в невисокосный год рождения существовать не может — считаем дату битой
            if (d > DateTime.DaysInMonth(y, m))
                return null;
        }

        return new Birthday(d, m, year);
    }

    /// <summary>
    /// Ближайший день рождения: в этом году, либо в следующем, если в этом уже прошёл.
    /// 29 февраля в невисокосный год считается 28-м.
    /// </summary>
    public static DateOnly GetNextBirthday(Birthday birthday, DateOnly today)
    {
        var next = birthday.InYear(today.Year);
        return next < today ? birthday.InYear(today.Year + 1) : next;
    }

    public static int GetDaysUntil(DateOnly nextBirthday, DateOnly today) =>
        nextBirthday.DayNumber - today.DayNumber;

    /// <summary>
    /// Ближайший ДР пользователя; <see cref="DateOnly.MaxValue"/>, если дата не указана —
    /// так «без даты» само уезжает в конец любой сортировки по ближайшему ДР.
    /// </summary>
    public static DateOnly GetNextBirthdayOrMax(UserModel user, DateOnly today) =>
        user.Profile?.Birthday is { } birthday ? GetNextBirthday(birthday, today) : DateOnly.MaxValue;

    /// <summary>
    /// Сколько лет исполнится в ближайший день рождения; null — если год рождения скрыт
    /// (возраст тогда просто не показываем).
    /// </summary>
    public static int? GetAge(Birthday birthday, DateOnly nextBirthday) =>
        birthday.Year is { } year ? nextBirthday.Year - year : null;

    /// <summary>
    /// «через 113 дней» / «сегодня» — относительная часть строки про ДР, одинаковая на
    /// всех экранах. Возраст приписывает сам экран: без года рождения его нет.
    /// </summary>
    public static string DescribeDaysUntil(int daysUntil) => daysUntil == 0
        ? "сегодня"
        : $"через {daysUntil} {PluralUtils.Days(daysUntil)}";

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
}
