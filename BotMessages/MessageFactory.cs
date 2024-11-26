using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
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
         MainMenuQuery => new MainMenuMessage(Logger),
         MyWishesQuery => new MyWishesMessage(Logger),
         CompactListQuery => new CompactListMessage(Logger),
         EditWishQuery => new EditingWishMessage(Logger),
         DeleteWishQuery => new ConfirmWishDeletionMessage(Logger),
         ConfirmWishDeletionQuery => new DeletedWishMessage(Logger),
         SetWishNameQuery => new SetWishNameMessage(Logger),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(Logger),
         SetWishMediaQuery => new SetWishMediaMessage(Logger),
         SetWishLinksQuery => new SetWishLinksMessage(Logger),
         CancelEditingWishQuery => new CancelledEditingWishMessage(Logger),
         FinishEditingWishQuery => new FinishedWishEditingMessage(Logger),
         FullListQuery => new FullListMessage(Logger),
         _ => new InvalidMessage(Logger),
      };

      if (botMessage is InvalidMessage)
         Logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
