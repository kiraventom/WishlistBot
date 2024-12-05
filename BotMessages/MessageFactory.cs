using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.BotMessages.Subscription;

namespace WishlistBot.BotMessages;

public class MessageFactory(ILogger logger, UsersDb usersDb)
{
   public BotMessage Build(IQuery query, BotUser user)
   {
      BotMessage botMessage = query switch
      {
         MainMenuQuery => new MainMenuMessage(logger),
         EditWishQuery => new EditWishMessage(logger),
         ConfirmDeleteWishQuery => new ConfirmDeleteWishMessage(logger),
         SetWishNameQuery => new SetWishNameMessage(logger),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(logger),
         SetWishMediaQuery => new SetWishMediaMessage(logger),
         SetWishLinksQuery => new SetWishLinksMessage(logger),
         CancelEditWishQuery => new CancelEditWishMessage(logger),
         DeleteWishQuery => new DeleteWishMessage(logger),
         FinishEditWishQuery => new FinishEditWishMessage(logger),
         CompactListQuery => new CompactListMessage(logger, usersDb),
         FullListQuery => new FullListMessage(logger, usersDb),
         ShowWishQuery => new ShowWishMessage(logger, usersDb),
         MySubscriptionsQuery => new MySubscriptionsMessage(logger, usersDb),
         MySubscribersQuery => new MySubscribersMessage(logger, usersDb),
         ConfirmUnsubscribeQuery => new ConfirmUnsubscribeMessage(logger, usersDb),
         UnsubscribeQuery => new UnsubscribeMessage(logger, usersDb),
         FinishSubscriptionQuery => new FinishSubscriptionMessage(logger, usersDb),
         SubscriberQuery => new SubscriberMessage(logger, usersDb),
         SubscriptionQuery => new SubscriptionMessage(logger, usersDb),
         ConfirmDeleteSubscriberQuery => new ConfirmDeleteSubscriberMessage(logger, usersDb),
         DeleteSubscriberQuery => new DeleteSubscriberMessage(logger, usersDb),
         _ => new InvalidMessage(logger),
      };

      if (botMessage is InvalidMessage)
         logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
