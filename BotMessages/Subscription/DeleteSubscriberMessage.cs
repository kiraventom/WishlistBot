using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

public class DeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<MySubscribersQuery>("К моим подписчикам");

      var sender = user;

      user = GetUser(user, parameters);

      Text.Italic("Вы удалили ")
         .InlineMention(user)
         .Italic(" из списка своих подписчиков.");

      user.Subscriptions.Remove(sender.SubscribeId);
      foreach (var claimedWish in sender.Wishes.Where(w => w.ClaimerId == user.SenderId))
      {
         claimedWish.ClaimerId = 0;
      }

      return Task.CompletedTask;
   }
}
