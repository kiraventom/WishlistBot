using Telegram.Bot;
using Serilog;
using WishlistBot.Users;

namespace WishlistBot;

public abstract class Command
{
   protected string CommandText => $"/{Name}";

   protected ILogger Logger { get; }
   protected ITelegramBotClient Client { get; }

   public abstract string Name { get; }

   protected Command(ILogger logger, ITelegramBotClient client)
   {
      Logger = logger;
      Client = client;
   }

   public abstract Task ExecuteAsync(BotUser user);

   public bool IsMatch(string text)
   {
      return text == CommandText;
   }
}
