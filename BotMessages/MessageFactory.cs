using Serilog;
using Telegram.Bot;
using WishlistBot.Users;
using WishlistBot.Queries;

namespace WishlistBot.BotMessages;

public class MessageFactory
{
   private ILogger Logger { get; }
   private ITelegramBotClient Client { get; }

   public MessageFactory(ILogger logger, ITelegramBotClient client)
   {
      Logger = logger;
      Client = client;
   }

   public BotMessage Build(IQuery query, BotUser user)
   {
      return query switch
      {
         MainMenuQuery => new MainMenuMessage(Logger, user),
         MyWishesQuery => new MyWishesMessage(Logger, user),
         _ => new InvalidMessage(Logger, user),
      };
   }
}
