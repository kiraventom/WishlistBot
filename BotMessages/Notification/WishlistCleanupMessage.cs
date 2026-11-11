using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Напоминание заглянуть в свой вишлист: после праздника там могли остаться
/// забронированные виши, которые друзья уже подарили.
/// </summary>
public class WishlistCleanupMessage(ILogger logger, bool snooze = false) : BotMessage(logger)
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

        Text.Verbatim("Возможно, в вашем вишлисте есть что-то, что вам уже подарили. Взглянете?");

        Text.AddNotificationsSettingsLink();

        Keyboard.AddButton<CompactListQuery>("\U0001f381 Открыть вишлист");

        if (!WishlistCleanupNotificationService.IsSnoozed(userContext, userId))
            Keyboard.AddButton<WishlistCleanupSnoozeQuery>();

        Keyboard.NewRow().AddButton<MainMenuQuery>("Закрыть");

        return Task.CompletedTask;
    }
}
