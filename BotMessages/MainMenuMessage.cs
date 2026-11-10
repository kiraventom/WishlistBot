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

        const string mutedSpeaker = "\U0001f507";
        const string dot = "⋅ ";

        if (!user.Settings.ReceiveNotifications || !user.Settings.SendNotifications)
            Text.LineBreak();

        if (!user.Settings.ReceiveNotifications)
            Text.LineBreak().ItalicBold($"{dot}Получение уведомлений о вишах друзей: ").Verbatim(mutedSpeaker);

        if (!user.Settings.SendNotifications)
            Text.LineBreak().ItalicBold($"{dot}Отправка уведомлений о вишах друзьям: ").Verbatim(mutedSpeaker);

        Text.LineBreak().LineBreak().Italic("Отправьте друзьям ссылку на свой вишлист в окне \"Профиль\"");

        return Task.CompletedTask;
    }
}
