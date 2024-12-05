using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class DeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<MySubscribersQuery>("К моим подписчикам");

      var sender = user;

      user = GetParameterUser(parameters);

      Text.Italic("Вы удалили ")
         .InlineMention(user)
         .Italic(" из списка своих подписчиков.");

      user.Subscriptions.Remove(sender.SubscribeId);

      return Task.CompletedTask;
   }
}
