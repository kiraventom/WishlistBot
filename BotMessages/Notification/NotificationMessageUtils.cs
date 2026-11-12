using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

public static class NotificationMessageUtils
{
    private const string NotificationsSettingsText = "\u2699\ufe0f Настроить уведомления";

    /// <summary>
    /// Последняя строка уведомления: ссылка на экран настроек уведомлений.
    /// Отбивается горизонтальной чертой — пустых строк не хватало, футер лип к тексту.
    /// </summary>
    public static MessageText AddNotificationsSettingsLink(this MessageText text) =>
        text.Divider()
            .InlineUrl(NotificationsSettingsText, DeepLink.BuildSettings());
}
