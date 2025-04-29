using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;

using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string giftEmoji = "\U0001f381";

        Keyboard
           .AddButton<CompactListQuery>($"{giftEmoji} Мои виши")
           .NewRow()
           .AddButton<MySubscriptionsQuery>()
           .AddButton<MySubscribersQuery>()
           .NewRow()
           .AddButton<SettingsQuery>();

        var user = userContext.Users.Include(u => u.Settings).First(u => u.UserId == userId);
        Text.Verbatim("Добро пожаловать в главное меню, ")
           .InlineMention(user)
           .Verbatim("!");

        const string mutedSpeaker = "\U0001f507";
        const string dot = "⋅ ";

        if (!user.Settings.ReceiveNotifications || !user.Settings.SendNotifications)
            Text.LineBreak();

        if (!user.Settings.ReceiveNotifications)
            Text.LineBreak().ItalicBold($"{dot}Получение уведомлений о вишах подписчиков: ").Verbatim(mutedSpeaker);

        if (!user.Settings.SendNotifications)
            Text.LineBreak().ItalicBold($"{dot}Отправка уведомлений о вишах подписчикам: ").Verbatim(mutedSpeaker);

        Text.LineBreak().LineBreak().Italic("Ссылку на вишлист можно скопировать в настройках");

        return Task.CompletedTask;
    }
}
