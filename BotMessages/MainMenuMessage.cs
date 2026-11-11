using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages;

public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string giftEmoji = "\U0001f381";

        var user = userContext.Users
            .Include(u => u.ClaimedWishes)
            .Include(u => u.Profile)
            .Include(u => u.Settings).First(u => u.UserId == userId);

        // Entering the wishlist from the main menu always starts from the first page
        Keyboard
           .AddButton<CompactListQuery>($"{giftEmoji} Виши", new QueryParameter(QueryParameterType.SetListPageTo, 0));

        if (user.ClaimedWishes.Any())
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
            Text.LineBreak().LineBreak()
                .Verbatim("🏆").Bold("ПОЛЬЗОВАТЕЛЬ #100").Verbatim("🏆");
        }

        const string mutedBell = "\U0001f515";
        const string dot = "⋅ ";

        // Подсказка про день рождения — выше статусов уведомлений и отбита строкой
        if (user.Profile.Birthday is null)
        {
            Text.LineBreak().LineBreak()
                .InlineUrl("\U0001f4c5 Можно указать дату дня рождения", DeepLink.BuildProfile());
        }

        if (!user.Settings.ReceiveNotifications
            || !user.Settings.SendNotifications
            || !user.Settings.BirthdayNotifications
            || !user.Settings.WishlistCleanupNotifications)
            Text.LineBreak();

        if (!user.Settings.ReceiveNotifications)
            Text.LineBreak().ItalicBold($"{dot}Вы не получаете уведомления от друзей ").Verbatim(mutedBell);

        if (!user.Settings.SendNotifications)
            Text.LineBreak().ItalicBold($"{dot}Друзья не получают уведомления от вас ").Verbatim(mutedBell);

        if (!user.Settings.BirthdayNotifications)
            Text.LineBreak().ItalicBold($"{dot}Вы не получаете напоминания о днях рождения ").Verbatim(mutedBell);

        if (!user.Settings.WishlistCleanupNotifications)
            Text.LineBreak().ItalicBold($"{dot}Вы не получаете напоминание о чистке вишлиста ").Verbatim(mutedBell);

        Text.LineBreak().LineBreak().Italic("Отправьте друзьям ссылку на свой вишлист в окне \"Профиль\"");

        return Task.CompletedTask;
    }
}
