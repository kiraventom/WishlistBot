using Serilog;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.BotMessages;

public class HelpMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<MainMenuQuery>("В главное меню");

        Text.Italic("Ответы на вопросы:")
           .LineBreak()
           .LineBreak()
           .InlineUrl("Перейти", @"https://telegra.ph/FAQ-Wishlist-BOT-12-27");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<MainMenuQuery>("В главное меню");

        Text.Italic("Ответы на вопросы:")
           .LineBreak()
           .LineBreak()
           .InlineUrl("Перейти", @"https://telegra.ph/FAQ-Wishlist-BOT-12-27");

        return Task.CompletedTask;
    }
}
