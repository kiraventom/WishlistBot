using Serilog;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.BotMessages.Subscription;

public class FailSubscriptionMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Italic("Некорректная ссылка на вишлист :(")
           .LineBreak()
           .Italic("Возможно, в ссылке опечатка или она неактуальна");

        Keyboard.AddButton<MainMenuQuery>("В главное меню");
        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Text.Italic("Некорректная ссылка на вишлист :(")
           .LineBreak()
           .Italic("Возможно, в ссылке опечатка или она неактуальна");

        Keyboard.AddButton<MainMenuQuery>("В главное меню");
        return Task.CompletedTask;
    }
}
