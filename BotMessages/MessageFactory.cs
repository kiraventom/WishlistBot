using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.BotMessages.Admin;
using WishlistBot.BotMessages.Admin.Broadcasts;

namespace WishlistBot.BotMessages;

public class MessageFactory(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb)
{
   public BotMessage Build(IQuery query, BotUser user)
   {
      BotMessage botMessage = query switch
      {
         MainMenuQuery => new MainMenuMessage(logger),
         ConfirmDeleteWishQuery => new ConfirmDeleteWishMessage(logger),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(logger),
         SetWishMediaQuery => new SetWishMediaMessage(logger),
         SetWishLinksQuery => new SetWishLinksMessage(logger),
         SetWishPriceQuery => new SetWishPriceMessage(logger),
         CancelEditWishQuery => new CancelEditWishMessage(logger),
         DeleteWishQuery => new DeleteWishMessage(logger),
         FinishEditWishQuery => new FinishEditWishMessage(logger),
         EditWishQuery => new EditWishMessage(logger, usersDb),
         SetWishNameQuery => new SetWishNameMessage(logger, usersDb),
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
         AdminMenuQuery => new AdminMenuMessage(logger, usersDb),
         BroadcastQuery => new BroadcastMessage(logger, usersDb, broadcastsDb),
         BroadcastsQuery => new BroadcastsMessage(logger, usersDb, broadcastsDb),
         ConfirmBroadcastQuery => new ConfirmBroadcastMessage(logger, broadcastsDb),
         ConfirmDeleteBroadcastQuery => new ConfirmDeleteBroadcastMessage(logger, usersDb, broadcastsDb),
         DeleteBroadcastQuery => new DeleteBroadcastMessage(logger, usersDb, broadcastsDb),
         FinishBroadcastQuery => new FinishBroadcastMessage(logger, usersDb, broadcastsDb),
         _ => new InvalidMessage(logger),
      };

      if (botMessage is InvalidMessage)
         logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);

      return botMessage;
   }
}
