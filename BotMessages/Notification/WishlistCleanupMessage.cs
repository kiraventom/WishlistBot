using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
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

        // Все действия — обычные инлайн-ссылки: «Взглянете» ведёт на свой вишлист,
        // «напомнить завтра» — диплинк, который откладывает напоминание
        var wishlistLink = DeepLink.BuildWishlist(user.SubscribeId[..20]);

        Text
            .Verbatim("Возможно, в вашем ")
            .InlineUrl("вишлисте", wishlistLink)
            .Verbatim(" есть что-то, что вам уже подарили. ")
            .InlineUrl("Взглянете", wishlistLink);

        if (!WishlistCleanupNotificationService.IsSnoozed(userContext, userId))
        {
            Text.Verbatim(" или ").InlineUrl("напомнить завтра", DeepLink.BuildCleanupSnooze());
        }

        Text.Verbatim("?");

        Text.AddNotificationsSettingsLink();

        Keyboard.AddButton<MainMenuQuery>("Главное меню");

        return Task.CompletedTask;
    }
}
