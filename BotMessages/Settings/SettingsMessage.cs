using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Settings;

[AllowedTypes(QueryParameterType.SetSettingsTo)]
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

        const string enabledSpeaker = "\U0001f50a";
        const string mutedSpeaker = "\U0001f507";
        const string enabledMark = "\u2705";
        const string disabledMark = "\u274c";

        var receiveNotificationsEmoji = user.Settings.ReceiveNotifications ? enabledSpeaker : mutedSpeaker;
        var sendNotificationsEmoji = user.Settings.SendNotifications ? enabledSpeaker : mutedSpeaker;

        var receiveNotificationsText = user.Settings.ReceiveNotifications
            ? $"{enabledMark} Сообщать мне об изменениях вишлистов друзей"
            : $"{disabledMark} Не сообщать мне об изменениях вишлистов друзей";

        var sendNotificationsText = user.Settings.SendNotifications
            ? $"{enabledMark} Сообщать друзьям об изменениях моего вишлиста"
            : $"{disabledMark} Не сообщать друзьям об изменениях моего вишлиста";

        Text
           .Bold("Настройки уведомлений:")
           .LineBreak().LineBreak()
           .Verbatim(receiveNotificationsText)
           .LineBreak()
           .Verbatim(sendNotificationsText);

        var settingsEnum = user.Settings.ToEnum();

        Keyboard.AddButton<SettingsQuery>(
           $"Мне: {receiveNotificationsEmoji}",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.ReceiveNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           $"Друзьям: {sendNotificationsEmoji}",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.SendNotifications)));

        Keyboard.NewRow().AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;

    }
}
