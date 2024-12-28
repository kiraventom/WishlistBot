using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class HelpMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
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
