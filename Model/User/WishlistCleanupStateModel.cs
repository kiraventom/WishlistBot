using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Состояние напоминаний о чистке вишлиста: когда последний раз напоминали
/// и не попросил ли пользователь напомнить позже («Напомнить завтра»).
/// Одна строка на пользователя.
/// </summary>
public class WishlistCleanupStateModel
{
    [Key] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    /// <summary>Дата последнего отправленного напоминания.</summary>
    public DateOnly? LastReminderDate { get; set; }

    /// <summary>Дата, на которую пользователь отложил напоминание.</summary>
    public DateOnly? NextReminderDate { get; set; }
}
