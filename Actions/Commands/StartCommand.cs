using Telegram.Bot;
using Serilog;
using WishlistBot.Database;
using WishlistBot.BotMessages;

namespace WishlistBot.Actions.Commands;

public class StartCommand : Command
{
   public override string Name => "/start";

   public StartCommand(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }

   public override async Task ExecuteAsync(BotUser user)
   {
      await Client.SendOrEditBotMessage(Logger, user, new MainMenuMessage(Logger, user), forceNewMessage: true);
   }
}
