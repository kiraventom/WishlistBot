namespace WishlistBot.Notification;

/// <summary>
/// Что за уведомление лежит в очереди <see cref="WishlistBot.Model.User.PendingNotificationModel"/>.
///
/// Первые пять значений — события друзей: данные для них берутся из
/// <see cref="WishlistBot.Model.User.NotificationModel"/> (ссылка на него лежит в очереди).
/// Остальные — фоновые рассылки и админские действия: у них нет источника-пользователя,
/// а всё, что нужно для восстановления сообщения, лежит в payload.
/// </summary>
public enum PendingNotificationType
{
    NewFriend = 0x1,
    NewWish = 0x2,
    WishEdit = 0x3,
    WishDelete = 0x4,
    WishPermanent = 0x5,

    BirthdayDigest = 0x10,
    WishlistCleanup = 0x11,
    WishlistFill = 0x12,

    /// <summary>Админская рассылка: payload — id рассылки.</summary>
    Broadcast = 0x20,

    /// <summary>Убрать доставленную рассылку из чата получателя: payload — id рассылки.</summary>
    BroadcastDelete = 0x21,
}
