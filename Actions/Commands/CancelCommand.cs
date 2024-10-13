using Telegram.Bot;
using Serilog;
using WishlistBot.Database;
using WishlistBot.BotMessages;

namespace WishlistBot.Actions.Commands;

public class CancelCommand : Command
{
   public override string Name => "/cancel";

   public CancelCommand(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }

   public override async Task ExecuteAsync(BotUser user)
   {
      if (user.BotState != BotState.AddingWish)
      {
         await Client.SendOrEditBotMessageAsync(Logger, user, new InvalidMessage(Logger, user), forceNewMessage: true);
         return;
      }

      user.CurrentWish = null;

      await Client.SendOrEditBotMessageAsync(Logger, user, new WishAddingCancelledMessage(Logger, user), forceNewMessage: true);
      await Client.SendOrEditBotMessageAsync(Logger, user, new MyWishesMessage(Logger, user), forceNewMessage: true);
   }
}
