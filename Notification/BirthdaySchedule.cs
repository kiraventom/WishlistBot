namespace WishlistBot.Notification;

/// <summary>
/// Когда отправляется дайджест дней рождения: 1 и 15 числа каждого месяца,
/// не раньше <see cref="RunTime"/> по локальному времени бота, и не чаще
/// одного раза в день (защита от повторной отправки после перезапуска).
/// </summary>
public static class BirthdaySchedule
{
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(10);

    public static bool IsScheduledDay(DateOnly date) => date.Day is 1 or 15;

    public static bool ShouldRun(DateTime now, DateOnly? lastRunDate)
    {
        var today = DateOnly.FromDateTime(now);

        return IsScheduledDay(today)
               && now.TimeOfDay >= RunTime
               && lastRunDate != today;
    }
}
