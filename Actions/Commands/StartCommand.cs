using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;

namespace WishlistBot.Actions.Commands;

public class StartCommand : Command
{
   public override string Name => "/start";

   public StartCommand(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
      user.QueryParams = null;
      await Client.SendOrEditBotMessage(Logger, user, new MainMenuMessage(Logger), forceNewMessage: true);
   }
}
