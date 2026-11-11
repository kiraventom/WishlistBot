using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Notification;

namespace WishlistBot.Model.User;

/// <summary>
/// Очередь уведомлений «на паузе». Запись живёт здесь, пока получатель активен в боте;
/// как только он молчит дольше <see cref="Notification.NotificationDispatchService.IdleMinutes"/>
/// минут, все накопленные записи уходят одним заходом и удаляются.
/// </summary>
[Index(nameof(RecepientId))]
public class PendingNotificationModel
{
    [Key] public int PendingNotificationId { get; set; }

    public int RecepientId { get; set; }

    public PendingNotificationType Type { get; set; }

    /// <summary>
    /// Событие друзей, из которого собирается сообщение. <see langword="null"/>
    /// у фоновых рассылок — у них нет ни источника, ни записи в <see cref="NotificationModel"/>.
    /// </summary>
    public int? NotificationId { get; set; }

    /// <summary>
    /// Всё, что нужно фоновой рассылке, чтобы восстановить сообщение: id друзей для
    /// дайджеста дней рождения, вид праздника для «заполнить вишлист». Строка, а не JSON,
    /// потому что данных ровно одно поле.
    /// </summary>
    public string Payload { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(RecepientId))]
    public UserModel Recepient { get; set; }

    [ForeignKey(nameof(NotificationId))]
    public NotificationModel Notification { get; set; }
}
