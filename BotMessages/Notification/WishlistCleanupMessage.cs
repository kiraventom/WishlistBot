using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Queries.Notification;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Напоминание заглянуть в свой вишлист: после праздника там могли остаться
/// забронированные виши, которые друзья уже подарили.
/// </summary>
public class WishlistCleanupMessage(ILogger logger, int claimedWishesCount) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Пора заглянуть в вишлист")
            .LineBreak().LineBreak()
            .Verbatim("Возможно, в вашем вишлисте есть что-то, что вам уже подарили. Взглянете?")
            .LineBreak().LineBreak()
            .Italic($"Забронированных вишей: {claimedWishesCount} {PluralUtils.Wishes(claimedWishesCount)}");

        Text.AddNotificationsSettingsLink();

        Keyboard
            .AddButton<CompactListQuery>("\U0001f381 Открыть вишлист")
            .AddButton<WishlistCleanupSnoozeQuery>()
            .NewRow()
            .AddButton<MainMenuQuery>("Закрыть");

        return Task.CompletedTask;
    }
}
