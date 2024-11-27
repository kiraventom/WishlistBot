using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.BotMessages.Subscription;

namespace WishlistBot.BotMessages;

public class MessageFactory
{
   private ILogger Logger { get; }
   private ITelegramBotClient Client { get; }
   private UsersDb UsersDb { get; }

   public MessageFactory(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      Logger = logger;
      Client = client;
      UsersDb = usersDb;
   }

   public BotMessage Build(IQuery query, BotUser user)
   {
      BotMessage botMessage = query switch
      {
         MainMenuQuery => new MainMenuMessage(Logger),
         CompactListQuery => new CompactListMessage(Logger, UsersDb),
         EditWishQuery => new EditWishMessage(Logger),
         ConfirmDeleteWishQuery => new ConfirmDeleteWishMessage(Logger),
         DeleteWishQuery => new DeleteWishMessage(Logger),
         SetWishNameQuery => new SetWishNameMessage(Logger),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(Logger),
         SetWishMediaQuery => new SetWishMediaMessage(Logger),
         SetWishLinksQuery => new SetWishLinksMessage(Logger),
         CancelEditWishQuery => new CancelEditWishMessage(Logger),
         FinishEditWishQuery => new FinishEditWishMessage(Logger),
         FullListQuery => new FullListMessage(Logger, UsersDb),
         ShowWishQuery => new ShowWishMessage(Logger, UsersDb),
         MySubscriptionsQuery => new MySubscriptionsMessage(Logger, UsersDb),
         ConfirmUnsubscribeQuery => new ConfirmUnsubscribeMessage(Logger, UsersDb),
         UnsubscribeQuery => new UnsubscribeMessage(Logger, UsersDb),
         _ => new InvalidMessage(Logger),
      };

      if (botMessage is InvalidMessage)
         Logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
