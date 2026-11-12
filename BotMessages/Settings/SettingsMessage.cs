using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Settings;

[AllowedTypes(QueryParameterType.SetSettingsTo)]
[MessageFor(typeof(SettingsQuery))]
public class SettingsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.Settings).First(u => u.UserId == userId);

        if (parameters.Pop(QueryParameterType.SetSettingsTo, out var newSettingsNum))
        {
            var newSettingsEnum = (SettingsEnum)newSettingsNum;
            user.Settings.SetFromEnum(newSettingsEnum);
        }

        const string enabledBell = "\U0001f514";
        const string disabledBell = "\U0001f515";

        var receiveFriendWishEmoji = user.Settings.ReceiveFriendWishNotifications ? enabledBell : disabledBell;
        var sendNotificationsEmoji = user.Settings.SendNotifications ? enabledBell : disabledBell;

        var receiveFriendWishText = user.Settings.ReceiveFriendWishNotifications
            ? $"{enabledBell} Сообщать мне об изменениях вишлистов друзей"
            : $"{disabledBell} Не сообщать мне об изменениях вишлистов друзей";

        var sendNotificationsText = user.Settings.SendNotifications
            ? $"{enabledBell} Сообщать друзьям об изменениях моего вишлиста"
            : $"{disabledBell} Не сообщать друзьям об изменениях моего вишлиста";

        var birthdayNotificationsEmoji = user.Settings.BirthdayNotifications ? enabledBell : disabledBell;

        var birthdayNotificationsText = user.Settings.BirthdayNotifications
            ? $"{enabledBell} Напоминать о днях рождения друзей"
            : $"{disabledBell} Не напоминать о днях рождения друзей";

        var wishlistCleanupEmoji = user.Settings.WishlistCleanupNotifications ? enabledBell : disabledBell;

        var wishlistCleanupText = user.Settings.WishlistCleanupNotifications
            ? $"{enabledBell} Напоминать о чистке вишлиста после праздников"
            : $"{disabledBell} Не напоминать о чистке вишлиста после праздников";

        var wishlistFillEmoji = user.Settings.WishlistFillNotifications ? enabledBell : disabledBell;

        var wishlistFillText = user.Settings.WishlistFillNotifications
            ? $"{enabledBell} Напоминать заполнить вишлист перед праздником"
            : $"{disabledBell} Не напоминать заполнить вишлист перед праздником";

        Text.Heading("Настройки уведомлений");

        Text.Bullets(
            receiveFriendWishText,
            sendNotificationsText,
            birthdayNotificationsText,
            wishlistCleanupText,
            wishlistFillText);

        var settingsEnum = user.Settings.ToEnum();

        var receiveFriendWishButton = user.Settings.ReceiveFriendWishNotifications
            ? $"{receiveFriendWishEmoji} Присылать мне"
            : $"{receiveFriendWishEmoji} Не присылать мне";

        var sendNotificationsButton = user.Settings.SendNotifications
            ? $"{sendNotificationsEmoji} Отправлять друзьям"
            : $"{sendNotificationsEmoji} Не отправлять друзьям";

        Keyboard.AddButton<SettingsQuery>(
           receiveFriendWishButton,
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.ReceiveFriendWishNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           sendNotificationsButton,
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.SendNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           $"{birthdayNotificationsEmoji} Дни рождения",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.BirthdayNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           $"{wishlistCleanupEmoji} Чистка вишлиста",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.WishlistCleanupNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           $"{wishlistFillEmoji} Заполнить вишлист",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.WishlistFillNotifications)));

        Keyboard.NewRow().AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;

    }
}
