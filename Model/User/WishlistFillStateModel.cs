using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Состояние напоминаний заполнить вишлист перед праздником: когда напоминали
/// в последний раз. Одна строка на пользователя, создаётся лениво.
/// </summary>
public class WishlistFillStateModel
{
    [Key] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    /// <summary>Дата последнего отправленного напоминания.</summary>
    public DateOnly? LastReminderDate { get; set; }
}
