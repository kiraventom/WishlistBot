using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Напоминание подготовить вишлист к празднику: приходит за 30 дней
/// до Нового года или личного дня рождения.
/// </summary>
public class WishlistFillMessage(ILogger logger, WishlistHolidayKind kind) : BotMessage(logger), INotificationMessage
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.AsNoTracking().First(u => u.UserId == userId);

        Text.Verbatim(kind == WishlistHolidayKind.Birthday
            ? "Уже скоро ваш день рождения! Самое время заполнить "
            : "Уже скоро Новый Год! Самое время заполнить ");

        Text
            .InlineUrl("вишлист", DeepLink.BuildWishlist(user.SubscribeId[..20]))
            .Verbatim("!");

        Text.AddNotificationsSettingsLink();

        Keyboard.AddButton<MainMenuQuery>("Главное меню");

        return Task.CompletedTask;
    }
}
