using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.Queries.Settings;
using WishlistBot.Queries.Profile;
using WishlistBot.Queries.EditWish;
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
        const string plusEmoji = "\u2795";
        const string linkEmoji = "\U0001f517";
        const string lockEmoji = "\U0001F512";
        const string friendsEmoji = "\U0001f465";

        var user = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.ClaimedWishes)
            .Include(u => u.Friends)
            .Include(u => u.Profile)
            .Include(u => u.Settings).First(u => u.UserId == userId);

        // «Включить» из главного меню: флаг включается сразу, экран настроек не открываем
        if (parameters.Pop(QueryParameterType.SetSettingsTo, out var newSettingsNum))
            user.Settings.SetFromEnum((SettingsEnum)newSettingsNum);

        var wishesCount = user.Wishes.Count;
        var claimsCount = user.ClaimedWishes.Count(wish => wish.HasVisibleClaim);
        var friendsCount = user.Friends.Count;

        // Виши: пустой вишлист сразу ведёт в окно добавления
        Keyboard.NewRow();

        if (wishesCount == 0)
            Keyboard.AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);
        else
            Keyboard.AddButton<CompactListQuery>($"{giftEmoji} Виши ({wishesCount})");

        if (claimsCount != 0)
            Keyboard.NewRow().AddButton<MyClaimsQuery>($"{lockEmoji} Брони ({claimsCount})");

        // Друзья: без друзей вместо списка — «поделиться вишлистом»
        Keyboard.NewRow();

        if (friendsCount == 0)
        {
            Keyboard.AddShareButton($"{linkEmoji} Отправить вишлист другу",
$@"
Мой вишлист в Вишлист БОТ:
{user.GetSubscribeLink()}");
        }
        else
            Keyboard.AddButton<MyFriendsQuery>($"{friendsEmoji} Друзья ({friendsCount})");

        Keyboard
           .NewRow()
           .AddButton<EditProfileQuery>()
           .NewRow()
           .AddButton<SettingsQuery>();

        // Приветствие — заголовком экрана
        Text.Heading(text => text
            .Verbatim("Добро пожаловать в главное меню, ")
            .InlineMention(user)
            .Verbatim("!"));

        if (user.UserId == 100)
            Text.Verbatim("\U0001f3c6").Bold("ПОЛЬЗОВАТЕЛЬ #100").Verbatim("\U0001f3c6").BlockBreak();

        // Подсказка про день рождения ведёт сразу в окно ввода даты, минуя профиль
        if (user.Profile.Birthday is null)
        {
            Text.Verbatim("\U0001f4c6 Не указана дата рождения ");

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

        // Ссылка на вишлист — подписью с кнопкой копирования, за чертой
        Text.Divider();
        Text.Verbatim("Ссылка на вишлист: ");

        Text.CopyButton($"{linkEmoji} Скопировать", user.GetSubscribeLink());

        return Task.CompletedTask;
    }
}
