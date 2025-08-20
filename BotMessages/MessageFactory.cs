using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.BotMessages.Admin;
using WishlistBot.BotMessages.Admin.Broadcasts;
using WishlistBot.BotMessages.Settings;
using WishlistBot.Queries.Settings;
using WishlistBot.Model;
using WishlistBot.Queries.Profile;
using WishlistBot.BotMessages.Profile;

namespace WishlistBot.BotMessages;

public class MessageFactory(ILogger logger)
{
   public BotMessage Build(IQuery query, UserContext userContext, string queryId)
   {
      BotMessage botMessage = query switch
      {
         MainMenuQuery => new MainMenuMessage(logger),
         MyClaimsQuery => new MyClaimsMessage(logger),
         ConfirmDeleteWishQuery => new ConfirmDeleteWishMessage(logger),
         SetWishDescriptionQuery => new SetWishDescriptionMessage(logger),
         SetWishMediaQuery => new SetWishMediaMessage(logger),
         SetWishLinksQuery => new SetWishLinksMessage(logger),
         SetWishPriceQuery => new SetWishPriceMessage(logger),
         DeleteWishQuery => new DeleteWishMessage(logger),
         EditWishQuery => new EditWishMessage(logger),
         SetWishNameQuery => new SetWishNameMessage(logger),
         CompactListQuery => new CompactListMessage(logger),
         ShowWishQuery => new ShowWishMessage(logger),
         MySubscriptionsQuery => new MySubscriptionsMessage(logger),
         MySubscribersQuery => new MySubscribersMessage(logger),
         ConfirmUnsubscribeQuery => new ConfirmUnsubscribeMessage(logger),
         UnsubscribeQuery => new UnsubscribeMessage(logger),
         FinishSubscriptionQuery => new FinishSubscriptionMessage(logger),
         SubscriberQuery => new SubscriberMessage(logger),
         SubscriptionQuery => new SubscriptionMessage(logger),
         ConfirmDeleteSubscriberQuery => new ConfirmDeleteSubscriberMessage(logger),
         DeleteSubscriberQuery => new DeleteSubscriberMessage(logger),
         AdminMenuQuery => new AdminMenuMessage(logger),
         BroadcastQuery => new BroadcastMessage(logger),
         BroadcastsQuery => new BroadcastsMessage(logger),
         ConfirmBroadcastQuery => new ConfirmBroadcastMessage(logger),
         ConfirmDeleteBroadcastQuery => new ConfirmDeleteBroadcastMessage(logger),
         DeleteBroadcastQuery => new DeleteBroadcastMessage(logger),
         FinishBroadcastQuery => new FinishBroadcastMessage(logger),
         SettingsQuery => new SettingsMessage(logger),
         ConfirmRegenerateLinkQuery => new ConfirmRegenerateLinkMessage(logger),
         EditProfileQuery => new EditProfileMessage(logger),
         SetProfileBirthdayQuery => new SetProfileBirthdayMessage(logger),
         SetProfileNotesQuery => new SetProfileNotesMessage(logger),
         _ => new InvalidMessage(logger),
      };

      if (botMessage is InvalidMessage)
         logger.Error("Failed to find message for query [{queryId}]", queryId);

      return botMessage;
   }
}
