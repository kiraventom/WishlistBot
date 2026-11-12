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

        const string enabledBell = "\U0001f514";

        var settingsEnum = user.Settings.ToEnum();

        Text.Heading("Настройки уведомлений");

        // Тумблеры — инлайн-ссылки, а не кнопки: колокольчик и название настройки ведут
        // на тот же экран с уже переключённым флагом (диплинк a=st_s=<значение>)
        Text.Table(table =>
        {
            table
                .Header(enabledBell, "Настройка")
                .Align(TableAlign.Center, TableAlign.Left);

            AddRow(table, settingsEnum, SettingsEnum.ReceiveFriendWishNotifications,
                user.Settings.ReceiveFriendWishNotifications, "Сообщать мне об изменениях вишлистов друзей");

            AddRow(table, settingsEnum, SettingsEnum.SendNotifications,
                user.Settings.SendNotifications, "Сообщать друзьям об изменениях моего вишлиста");

            AddRow(table, settingsEnum, SettingsEnum.BirthdayNotifications,
                user.Settings.BirthdayNotifications, "Напоминать о днях рождения друзей");

            AddRow(table, settingsEnum, SettingsEnum.WishlistCleanupNotifications,
                user.Settings.WishlistCleanupNotifications, "Напоминать о чистке вишлиста после праздников");

            AddRow(table, settingsEnum, SettingsEnum.WishlistFillNotifications,
                user.Settings.WishlistFillNotifications, "Напоминать заполнить вишлист перед праздником");
        });

        Keyboard.AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }

    private static void AddRow(
        TableBuilder table,
        SettingsEnum settingsEnum,
        SettingsEnum flag,
        bool isEnabled,
        string name)
    {
        var link = DeepLink.BuildSettings(settingsEnum ^ flag);
        var bell = isEnabled ? "\U0001f514" : "\U0001f515";

        table.Row(
            cell => cell.InlineUrl(bell, link),
            cell => cell.InlineUrl(name, link));
    }
}
