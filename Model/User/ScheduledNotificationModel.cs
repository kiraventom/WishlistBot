using System.ComponentModel.DataAnnotations;

namespace WishlistBot.Model.User;

/// <summary>
/// Состояние фоновых рассылок: когда в последний раз отрабатывал планировщик.
/// Нужно, чтобы при перезапуске бота рассылка не ушла повторно.
/// </summary>
public class ScheduledNotificationModel
{
    /// <summary>Имя рассылки, например <c>birthdays</c>.</summary>
    [Key] public string Name { get; set; }

    public DateOnly? LastRunDate { get; set; }
}
