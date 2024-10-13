using Serilog;
using Telegram.Bot;
using WishlistBot.Database;
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
      BotMessage botMessage = query switch
      {
         MainMenuQuery => new MainMenuMessage(Logger, user),
         MyWishesQuery => new MyWishesMessage(Logger, user),
         AddWishQuery => new AddWishMessage(Logger, user),
         CompactListMyWishesQuery => new CompactListMyWishesMessage(Logger, user),
         FullListMyWishesQuery => new FullListMyWishesMessage(Logger, user),
         _ => new InvalidMessage(Logger, user),
      };

      if (botMessage is InvalidMessage)
         Logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
