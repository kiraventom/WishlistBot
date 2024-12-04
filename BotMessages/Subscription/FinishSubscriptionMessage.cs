using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class FinishSubscriptionMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var sender = user;
      user = GetParameterUser(parameters);

      if (sender.Subscriptions.Contains(user.SubscribeId))
      {
         Text.Italic("Вы уже подписаны на вишлист ")
            .InlineMention(user)
            .Italic(".");
      }
      else
      {
         Text.Italic("Вы успешно подписались на вишлист ")
            .InlineMention(user)
            .Italic("!");

         sender.Subscriptions.Add(user.SubscribeId);
      }

      Keyboard
         .AddButton<CompactListQuery>($"Открыть вишлист {user.FirstName}",
                                      QueryParameter.ReadOnly,
                                      new QueryParameter(QueryParameterType.SetUserTo, user.SenderId))
         .NewRow()
         .AddButton<MySubscriptionsQuery>("К моим подпискам");
   }
}
