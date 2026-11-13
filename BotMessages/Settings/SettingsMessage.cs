using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Text;
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

        var settingsEnum = user.Settings.ToEnum();

        Text.Heading("Настройки уведомлений");

        // Чекбокс-список: состояние видно галочкой, а название настройки — кнопка в тексте
        // (tg-button), которая переключает флаг. Отдельных кнопок-тумблеров нет.
        var settings = new (SettingsEnum Flag, bool IsEnabled, string Name)[]
        {
            (SettingsEnum.ReceiveFriendWishNotifications, user.Settings.ReceiveFriendWishNotifications,
                "Сообщать мне об изменениях вишлистов друзей"),
            (SettingsEnum.SendNotifications, user.Settings.SendNotifications,
                "Сообщать друзьям об изменениях моего вишлиста"),
            (SettingsEnum.BirthdayNotifications, user.Settings.BirthdayNotifications,
                "Напоминать о днях рождения друзей"),
            (SettingsEnum.WishlistCleanupNotifications, user.Settings.WishlistCleanupNotifications,
                "Напоминать о чистке вишлиста после праздников"),
            (SettingsEnum.WishlistFillNotifications, user.Settings.WishlistFillNotifications,
                "Напоминать заполнить вишлист перед праздником"),
        };

        Text.TaskList(
            settings.Length,
            index => settings[index].IsEnabled,
            (line, index) => line.Button(settings[index].Name, Keyboard.AddTextButton<SettingsQuery>(
                parameters: new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ settings[index].Flag)))));

        Keyboard.AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }
}
