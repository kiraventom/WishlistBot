using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

public class FailSubscriptionMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Text.Italic("Некорректная ссылка на вишлист :(")
         .LineBreak()
         .Italic("Возможно, в ссылке опечатка или она неактуальна");

      Keyboard.AddButton<MainMenuQuery>("В главное меню");
   }
}
