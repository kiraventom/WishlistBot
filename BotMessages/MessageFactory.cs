using Serilog;
using Telegram.Bot;
using WishlistBot.Database;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.EditingWish;

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
         CompactListMyWishesQuery => new CompactListMyWishesMessage(Logger, user),
         FullListMyWishesQuery => new FullListMyWishesMessage(Logger, user),
         EditWishQuery => new EditingWishMessage(Logger, user),
         SetWishNameQuery => new SetWishNameMessage(Logger, user),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(Logger, user),
         SetWishMediaQuery => new SetWishMediaMessage(Logger, user),
         SetWishLinksQuery => new SetWishLinksMessage(Logger, user),
         CancelEditingWishQuery => new CancelledEditingWishMessage(Logger, user),
         FinishEditingWishQuery => new FinishedWishEditingMessage(Logger, user),
         _ => new InvalidMessage(Logger, user),
      };

      if (botMessage is InvalidMessage)
         Logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
