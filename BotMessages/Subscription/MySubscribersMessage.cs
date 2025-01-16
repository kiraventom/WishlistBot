using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ReadOnly)]
public class MySubscribersMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var subscribers = Users
         .Where(u => u.Subscriptions.Contains(user.SubscribeId))
         .ToList();

      var totalCount = subscribers.Count;

      Text.Bold(totalCount == 0 ? "У вас пока нет подписчиков :(" : "Ваши подписчики:");

      ListMessageUtils.AddListControls<MySubscribersQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
      {
         var subscriber = subscribers[itemIndex];

         Keyboard.AddButton<SubscriberQuery>(
            subscriber.FirstName,
            QueryParameter.ReadOnly,
            new QueryParameter(QueryParameterType.SetUserTo, subscriber.SenderId),
            new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
      });

      return Task.CompletedTask;
   }
}
