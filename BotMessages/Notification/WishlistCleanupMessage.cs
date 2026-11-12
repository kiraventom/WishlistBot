using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Напоминание заглянуть в свой вишлист: после праздника там могли остаться
/// забронированные виши, которые друзья уже подарили.
/// </summary>
public class WishlistCleanupMessage(ILogger logger, bool snooze = false) : BotMessage(logger), INotificationMessage
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        // Напоминание пришло по нажатию «Напомнить завтра»: откладываем его на завтра
        // и больше кнопку не показываем — вместо отдельного экрана короткий тост.
        if (snooze)
        {
            WishlistCleanupNotificationService.Snooze(userContext, userId, DateOnly.FromDateTime(DateTime.Now));
            ToastText = $"Напоминание придет завтра в {WishlistCleanupSchedule.RunTime:hh\\:mm}";
        }

        var user = userContext.Users.AsNoTracking().First(u => u.UserId == userId);

        // «вишлисте» ведёт на свой вишлист, дальше два действия — кнопками в тексте
        Text
            .Verbatim("Возможно, в вашем ")
            .InlineUrl("вишлисте", DeepLink.BuildWishlist(user.SubscribeId[..20]))
            .Verbatim(" есть что-то, что вам уже подарили. ");

        Text.Button("Взглянете", Keyboard.AddTextButton<CompactListQuery>());

        if (!WishlistCleanupNotificationService.IsSnoozed(userContext, userId))
        {
            Text.Verbatim(" или ");
            Text.Button("напомнить завтра", Keyboard.AddTextButton<WishlistCleanupSnoozeQuery>());
        }

        Text.Verbatim("?");

        Text.AddNotificationsSettingsLink();

        Keyboard.AddButton<CompactListQuery>("\U0001f381 Открыть вишлист");

        Keyboard.NewRow().AddButton<MainMenuQuery>("Главное меню");

        return Task.CompletedTask;
    }
}
