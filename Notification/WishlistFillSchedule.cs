using WishlistBot.Model.User;

namespace WishlistBot.Notification;

public enum WishlistHolidayKind
{
    NewYear,
    Birthday
}

/// <summary>Праздник, к которому напоминаем подготовить вишлист.</summary>
public sealed record WishlistHoliday(WishlistHolidayKind Kind, DateOnly Date);

/// <summary>
/// Когда напоминать заполнить вишлист: за <see cref="DaysBeforeHoliday"/> дней до
/// праздника — Нового года (1 января у всех) или личного дня рождения, если он указан.
/// О празднике напоминаем один раз: отметка хранит дату праздника, а не дату отправки.
/// </summary>
public static class WishlistFillSchedule
{
    /// <summary>За сколько дней до праздника напоминаем.</summary>
    public const int DaysBeforeHoliday = 30;

    /// <summary>Не раньше этого времени по локальному времени бота.</summary>
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(10);

    public static bool IsRunTime(DateTime now) => now.TimeOfDay >= RunTime;

    /// <summary>
    /// Ближайший праздник, до которого осталось не больше <see cref="DaysBeforeHoliday"/>
    /// дней: Новый год или день рождения, что наступит раньше.
    /// </summary>
    public static WishlistHoliday? GetUpcomingHoliday(DateOnly? birthday, DateOnly today)
    {
        var holidays = new List<WishlistHoliday>
        {
            new(WishlistHolidayKind.NewYear, new DateOnly(today.Year, 1, 1)),
            new(WishlistHolidayKind.NewYear, new DateOnly(today.Year + 1, 1, 1))
        };

        if (birthday is { } date)
            holidays.Add(new WishlistHoliday(WishlistHolidayKind.Birthday, BirthdayUtils.GetNextBirthday(date, today)));

        var upcoming = holidays
            .Where(holiday => holiday.Date > today && holiday.Date.DayNumber - today.DayNumber <= DaysBeforeHoliday)
            .ToList();

        return upcoming.Count == 0 ? null : upcoming.MinBy(holiday => holiday.Date);
    }

    /// <summary>Пора ли напоминать: праздник уже в окне и о нём ещё не напоминали.</summary>
    public static bool IsDue(DateOnly? birthday, DateOnly today, DateOnly? lastReminderHoliday)
    {
        if (GetUpcomingHoliday(birthday, today) is not { } holiday)
            return false;

        return lastReminderHoliday != holiday.Date;
    }
}
