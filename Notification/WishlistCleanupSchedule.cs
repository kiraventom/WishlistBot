using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>
/// Когда напоминать о чистке вишлиста. Отсчёт идёт от праздников: Новый год
/// (1 января у всех) и личный день рождения, если он указан. Через
/// <see cref="DaysAfterHoliday"/> дней после праздника напоминаем, что в вишлисте
/// остались забронированные виши, и держим напоминание актуальным ещё
/// <see cref="ReminderWindow"/> дней — если бот лежал или пользователь пришёл позже,
/// он всё равно получит его, но не задним числом через полгода.
/// </summary>
public static class WishlistCleanupSchedule
{
    /// <summary>Сколько дней ждём после праздника.</summary>
    public const int DaysAfterHoliday = 30;

    /// <summary>Сколько дней после срока напоминание ещё актуально.</summary>
    public const int ReminderWindow = 30;

    /// <summary>Не раньше этого времени по локальному времени бота.</summary>
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(10);

    public static bool IsRunTime(DateTime now) => now.TimeOfDay >= RunTime;

    /// <summary>
    /// Праздники, от которых считается срок: Новый год за текущий и прошлый год,
    /// плюс день рождения пользователя в тех же годах.
    /// </summary>
    public static List<DateOnly> GetHolidays(DateOnly? birthday, DateOnly today)
    {
        var holidays = new List<DateOnly>();

        for (var year = today.Year - 1; year <= today.Year; ++year)
        {
            holidays.Add(new DateOnly(year, 1, 1));

            if (birthday is { } date)
                holidays.Add(BirthdayUtils.InYear(date, year));
        }

        return holidays;
    }

    /// <summary>
    /// Срок напоминания, который действует сегодня. Берём последний наступивший срок
    /// в пределах окна; если все сроки уже просрочены — напоминать не о чем.
    /// </summary>
    public static DateOnly? GetDueTriggerDate(DateOnly? birthday, DateOnly today)
    {
        var due = GetHolidays(birthday, today)
            .Select(holiday => holiday.AddDays(DaysAfterHoliday))
            .Where(trigger => trigger <= today && today.DayNumber - trigger.DayNumber <= ReminderWindow)
            .ToList();

        return due.Count == 0 ? null : due.Max();
    }

    /// <summary>
    /// Пора ли напоминать этому пользователю: либо он сам попросил напомнить позже
    /// («Напомнить завтра»), либо наступил срок праздника, о котором ему ещё не напоминали.
    /// </summary>
    public static bool IsDue(DateOnly? birthday, DateOnly today, DateOnly? lastReminderDate, DateOnly? nextReminderDate)
    {
        if (nextReminderDate is { } next && next <= today)
            return true;

        if (GetDueTriggerDate(birthday, today) is not { } trigger)
            return false;

        return lastReminderDate is null || lastReminderDate < trigger;
    }
}
