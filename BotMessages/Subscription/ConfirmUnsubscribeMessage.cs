using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(SubscriberMessage))]
public class ConfirmUnsubscribeMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<UnsubscribeQuery>()
         .NewRow()
         .AddButton<CompactListQuery>("Отмена \u274c");

      user = GetUser(user, parameters);

      Text.Italic("Действительно отписаться от ")
         .InlineMention(user)
         .Italic("?");

      return Task.CompletedTask;
   }
}
