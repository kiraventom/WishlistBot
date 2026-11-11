using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Ответ на «Напомнить завтра»: откладывает напоминание о чистке вишлиста
/// на следующий день и подтверждает это пользователю.
/// </summary>
public class WishlistCleanupSnoozedMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        WishlistCleanupNotificationService.Snooze(userContext, userId, DateOnly.FromDateTime(DateTime.Now));

        Text.Bold("Хорошо, напомню завтра")
            .LineBreak().LineBreak()
            .Verbatim("Заглянуть в вишлист можно и сейчас — вдруг что-то уже подарили.");

        Keyboard
            .AddButton<CompactListQuery>("\U0001f381 Открыть вишлист")
            .NewRow()
            .AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }
}
