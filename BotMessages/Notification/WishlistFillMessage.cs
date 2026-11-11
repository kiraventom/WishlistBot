using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Напоминание подготовить вишлист к празднику: приходит за 30 дней
/// до Нового года или личного дня рождения.
/// </summary>
public class WishlistFillMessage(ILogger logger, WishlistHolidayKind kind) : BotMessage(logger), INotificationMessage
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Verbatim(kind == WishlistHolidayKind.Birthday
            ? "Уже скоро ваш день рождения! Самое время заполнить вишлист"
            : "Уже скоро Новый Год! Самое время заполнить вишлист");

        var user = userContext.Users.First(u => u.UserId == userId);

        Text.LineBreak()
            .InlineUrl("\U0001f381 Открыть вишлист", DeepLink.BuildWishlist(user.SubscribeId[..20]))
            .LineBreak()
            .InlineUrl("Закрыть", DeepLink.BuildMainMenu());

        Text.AddNotificationsSettingsLink();

        return Task.CompletedTask;
    }
}
