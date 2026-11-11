using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Состояние напоминаний заполнить вишлист перед праздником: о каком празднике
/// пользователю уже напомнили. Одна строка на пользователя, создаётся лениво.
/// </summary>
public class WishlistFillStateModel
{
    [Key] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    /// <summary>Дата праздника, о котором уже напомнили.</summary>
    public DateOnly? LastReminderHoliday { get; set; }
}
