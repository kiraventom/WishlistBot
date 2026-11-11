using WishlistBot.Model.User;

namespace WishlistBot.Notification;

public enum WishlistHolidayKind
{
    NewYear,
    Birthday
}

/// <summary>Праздник, к которому напоминаем подготовить вишлист.</summary>
public sealed record WishlistHoliday(WishlistHolidayKind Kind, DateOnly Date)
{
    /// <summary>День, в который напоминание о празднике становится актуальным.</summary>
    public DateOnly DueDate => Date.AddDays(-WishlistFillSchedule.DaysBeforeHoliday);
}

/// <summary>
/// Когда напоминать заполнить вишлист: **один раз** за <see cref="DaysBeforeHoliday"/>
/// дней до праздника — Нового года (1 января у всех) или личного дня рождения, если он
/// указан. Напоминание не повторяется: если любое напоминание о празднике уходило
/// в последние <see cref="MinDaysBetweenReminders"/> дней (даже про другой праздник),
/// новое не отправляем.
/// </summary>
public static class WishlistFillSchedule
{
    /// <summary>За сколько дней до праздника напоминаем.</summary>
    public const int DaysBeforeHoliday = 30;

    /// <summary>Не чаще одного напоминания за столько дней.</summary>
    public const int MinDaysBetweenReminders = 30;

    /// <summary>Не раньше этого времени по локальному времени бота.</summary>
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(10);

    public static bool IsRunTime(DateTime now) => now.TimeOfDay >= RunTime;

    /// <summary>
    /// Праздник, о котором пора напомнить: срок уже наступил, сам праздник ещё нет.
    /// Если сроков наступило несколько (например, Новый год и день рождения рядом),
    /// берём тот, чей срок наступил раньше — напоминание будет про него.
    /// </summary>
    public static WishlistHoliday GetDueHoliday(DateOnly? birthday, DateOnly today)
    {
        var holidays = new List<WishlistHoliday>
        {
            new(WishlistHolidayKind.NewYear, new DateOnly(today.Year, 1, 1)),
            new(WishlistHolidayKind.NewYear, new DateOnly(today.Year + 1, 1, 1))
        };

        if (birthday is { } date)
            holidays.Add(new WishlistHoliday(WishlistHolidayKind.Birthday, BirthdayUtils.GetNextBirthday(date, today)));

        var due = holidays
            .Where(holiday => holiday.Date > today && holiday.DueDate <= today)
            .ToList();

        return due.Count == 0 ? null : due.MinBy(holiday => holiday.DueDate);
    }

    /// <summary>
    /// Пора ли напоминать: срок праздника наступил и за последние
    /// <see cref="MinDaysBetweenReminders"/> дней напоминаний не было.
    /// </summary>
    public static bool IsDue(DateOnly? birthday, DateOnly today, DateOnly? lastReminderDate)
    {
        if (GetDueHoliday(birthday, today) is null)
            return false;

        return lastReminderDate is null
               || today.DayNumber - lastReminderDate.Value.DayNumber >= MinDaysBetweenReminders;
    }
}
