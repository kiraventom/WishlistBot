using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.Queries.Settings;
using WishlistBot.Queries.Profile;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Text;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetSettingsTo)]
[MessageFor(typeof(MainMenuQuery))]
public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string giftEmoji = "\U0001f381";
        const string mutedBell = "\U0001f515";

        var user = userContext.Users
            .Include(u => u.ClaimedWishes)
            .Include(u => u.Profile)
            .Include(u => u.Settings).First(u => u.UserId == userId);

        // «Включить» из главного меню: флаг включается сразу, экран настроек не открываем
        if (parameters.Pop(QueryParameterType.SetSettingsTo, out var newSettingsNum))
            user.Settings.SetFromEnum((SettingsEnum)newSettingsNum);

        // Страницу не передаём: вишлист открывается на той, где зритель его оставил
        Keyboard
           .AddButton<CompactListQuery>($"{giftEmoji} Виши");

        if (user.ClaimedWishes.Any(wish => wish.HasVisibleClaim))
        {
            Keyboard
                .NewRow()
                .AddButton<MyClaimsQuery>();
        }

        Keyboard
           .NewRow()
           .AddButton<MyFriendsQuery>()
           .NewRow()
           .AddButton<EditProfileQuery>()
           .NewRow()
           .AddButton<SettingsQuery>();

        Text.Verbatim("Добро пожаловать в главное меню, ")
           .InlineMention(user)
           .Verbatim("!");

        if (user.UserId == 100)
        {
            Text.BlockBreak()
                .Verbatim("\U0001f3c6").Bold("ПОЛЬЗОВАТЕЛЬ #100").Verbatim("\U0001f3c6");
        }

        // Подсказка про день рождения ведёт сразу в окно ввода даты, минуя профиль
        if (user.Profile.Birthday is null)
        {
            Text.BlockBreak()
                .Verbatim("\U0001f4c6 Не указана дата рождения ");

            Text.Button("Указать", Keyboard.AddTextButton<SetProfileBirthdayQuery>());
        }

        // Статусы выключенных уведомлений: короткая подпись + кнопка «Включить»,
        // которая включает категорию сразу, не открывая настройки
        var mutedStatuses = new List<(SettingsEnum Flag, string Name)>();

        if (!user.Settings.ReceiveFriendWishNotifications)
            mutedStatuses.Add((SettingsEnum.ReceiveFriendWishNotifications, "Уведомления от друзей"));

        if (!user.Settings.SendNotifications)
            mutedStatuses.Add((SettingsEnum.SendNotifications, "Уведомления друзьям"));

        if (!user.Settings.BirthdayNotifications)
            mutedStatuses.Add((SettingsEnum.BirthdayNotifications, "Напоминания о днях рождения"));

        if (!user.Settings.WishlistCleanupNotifications)
            mutedStatuses.Add((SettingsEnum.WishlistCleanupNotifications, "Напоминание о чистке вишлиста"));

        if (!user.Settings.WishlistFillNotifications)
            mutedStatuses.Add((SettingsEnum.WishlistFillNotifications, "Напоминание заполнить вишлист"));

        if (mutedStatuses.Count != 0)
            Text.BlockBreak();

        var settingsEnum = user.Settings.ToEnum();

        for (var i = 0; i < mutedStatuses.Count; ++i)
        {
            var (flag, name) = mutedStatuses[i];

            if (i > 0)
                Text.LineBreak();

            Text.Verbatim($"{mutedBell} {name} ");

            Text.Button("Включить", Keyboard.AddTextButton<MainMenuQuery>(
                parameters: new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum | flag))));
        }

        Text.BlockBreak().Italic("Отправьте друзьям ссылку на свой вишлист в окне \"Профиль\"");

        return Task.CompletedTask;
    }
}
