using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages;

[MessageFor(typeof(MainMenuQuery))]
public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string giftEmoji = "\U0001f381";

        var user = userContext.Users
            .Include(u => u.ClaimedWishes)
            .Include(u => u.Profile)
            .Include(u => u.Settings).First(u => u.UserId == userId);

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
                .Verbatim("🏆").Bold("ПОЛЬЗОВАТЕЛЬ #100").Verbatim("🏆");
        }

        const string mutedBell = "\U0001f515";
        const string dot = "⋅ ";

        // Подсказка про день рождения — выше статусов уведомлений и отбита строкой
        if (user.Profile.Birthday is null)
        {
            Text.BlockBreak()
                .InlineUrl("\U0001f4c5 Можно указать дату дня рождения", DeepLink.BuildProfile());
        }

        if (!user.Settings.ReceiveFriendWishNotifications
            || !user.Settings.SendNotifications
            || !user.Settings.BirthdayNotifications
            || !user.Settings.WishlistCleanupNotifications
            || !user.Settings.WishlistFillNotifications)
            Text.BlockBreak();

        // Статусы идут строками одного абзаца: первая без переноса, остальные — с ним
        var mutedStatuses = new List<string>();

        if (!user.Settings.ReceiveFriendWishNotifications)
            mutedStatuses.Add($"{dot}Вы не получаете уведомления от друзей ");

        if (!user.Settings.SendNotifications)
            mutedStatuses.Add($"{dot}Друзья не получают уведомления от вас ");

        if (!user.Settings.BirthdayNotifications)
            mutedStatuses.Add($"{dot}Вы не получаете напоминания о днях рождения ");

        if (!user.Settings.WishlistCleanupNotifications)
            mutedStatuses.Add($"{dot}Вы не получаете напоминание о чистке вишлиста ");

        if (!user.Settings.WishlistFillNotifications)
            mutedStatuses.Add($"{dot}Вы не получаете напоминание заполнить вишлист ");

        for (var i = 0; i < mutedStatuses.Count; ++i)
        {
            if (i > 0)
                Text.LineBreak();

            Text.ItalicBold(mutedStatuses[i]).Verbatim(mutedBell);
        }

        Text.BlockBreak().Italic("Отправьте друзьям ссылку на свой вишлист в окне \"Профиль\"");

        return Task.CompletedTask;
    }
}
