using Telegram.Bot;
using Serilog;
using WishlistBot.Users;
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
      user.LastBotMessageId = -1; // Never edit message on /start command
      await Client.SendOrEditBotMessageAsync(Logger, user, new MainMenuMessage(Logger, user));
   }
}
