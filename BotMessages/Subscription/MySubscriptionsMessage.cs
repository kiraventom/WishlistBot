using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MySubscriptionsMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var totalCount = user.Subscriptions.Count;

      Text.Bold(totalCount == 0 ? "Вы ещё ни на кого не подписаны :(" : "Ваши подписки:");

      ListMessageUtils.AddListControls<MySubscriptionsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
      {
         var subscribeId = user.Subscriptions[itemIndex];
         var userWeSubscribedTo = Users.FirstOrDefault(u => u.SubscribeId == subscribeId);
         if (userWeSubscribedTo is null)
         {
            Logger.Error("Users DB does not contain user wish subscribe id [{subscribeId}]", subscribeId);
            return;
         }

         Keyboard.AddButton<SubscriptionQuery>(
            userWeSubscribedTo.FirstName,
            QueryParameter.ReadOnly,
            new QueryParameter(QueryParameterType.SetUserTo, userWeSubscribedTo.SenderId),
            new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
      });

      return Task.CompletedTask;
   }
}
