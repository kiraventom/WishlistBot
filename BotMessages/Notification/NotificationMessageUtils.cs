using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

public static class NotificationMessageUtils
{
    private const string DisableNotificationsText = "выключить эти уведомления";

    /// <summary>
    /// Последняя строка уведомления: ссылка на экран настроек, чтобы можно было отписаться.
    /// </summary>
    public static MessageText AddDisableNotificationsLink(this MessageText text) =>
        text.LineBreak()
            .LineBreak()
            .InlineUrl(DisableNotificationsText, DeepLink.BuildSettings());
}
