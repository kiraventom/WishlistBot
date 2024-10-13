using Telegram.Bot;
using Serilog;
using WishlistBot.Database;
using WishlistBot.BotMessages;

namespace WishlistBot.Actions.Commands;

public class SaveCommand : Command
{
   public override string Name => "/save";

   public SaveCommand(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }

   public override async Task ExecuteAsync(BotUser user)
   {
      if (user.BotState != BotState.AddingWish)
      {
         await Client.SendOrEditBotMessageAsync(Logger, user, new InvalidMessage(Logger, user), forceNewMessage: true);
         return;
      }

      var wish = user.CurrentWish;
      user.CurrentWish = null;

      if (!wish.Messages.Any())
      {
         Logger.Warning("No messages was sent, aborting adding wish");
         await Client.SendOrEditBotMessageAsync(Logger, user, new WishAddingFailedMessage(Logger, user), forceNewMessage: true);
         await Client.SendOrEditBotMessageAsync(Logger, user, new MyWishesMessage(Logger, user), forceNewMessage: true);
         return;
      }

      user.Wishes.Add(wish);

      await Client.SendOrEditBotMessageAsync(Logger, user, new WishAddedMessage(Logger, user), forceNewMessage: true);
   }
}
