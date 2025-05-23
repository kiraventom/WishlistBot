using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Settings;

[AllowedTypes(QueryParameterType.SetSettingsTo, QueryParameterType.RegenerateLink)]
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

        if (parameters.Pop(QueryParameterType.RegenerateLink))
        {
            var newSubscribeId = Guid.NewGuid().ToString("N");
            user.SubscribeId = newSubscribeId;
        }

        const string regenerateEmoji = "\U0001f501";

        const string enabledSpeaker = "\U0001f50a";
        const string mutedSpeaker = "\U0001f507";
        const string enabledStr = "Вкл.";
        const string mutedStr = "Выкл.";
        const string dot = "⋅ ";

        var receiveNotificationsEmoji = user.Settings.ReceiveNotifications ? enabledSpeaker : mutedSpeaker;
        var sendNotificationsEmoji = user.Settings.SendNotifications ? enabledSpeaker : mutedSpeaker;

        var receiveNotificationsStr = user.Settings.ReceiveNotifications ? enabledStr : mutedStr;
        var sendNotificationsStr = user.Settings.SendNotifications ? enabledStr : mutedStr;

        Text
           .Bold("Настройки бота:")
           .LineBreak().Bold(dot).Bold("Получение уведомлений: ").Verbatim(receiveNotificationsStr)
           .LineBreak().Bold(dot).Bold("Отправка уведомлений: ").Verbatim(sendNotificationsStr);

        var settingsEnum = user.Settings.ToEnum();

        Keyboard.AddButton<SettingsQuery>(
           $"Получение уведомлений: {receiveNotificationsEmoji}",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.ReceiveNotifications)));

        Keyboard.NewRow().AddButton<SettingsQuery>(
           $"Отправка уведомлений: {sendNotificationsEmoji}",
           new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.SendNotifications)));

        Keyboard.NewRow().AddButton<ConfirmRegenerateLinkQuery>($"{regenerateEmoji} Изменить ссылку на вишлист");
        Keyboard.NewRow().AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;

    }
}
