using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class ConfirmUnsubscribeMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<UnsubscribeQuery>()
         .NewRow()
         .AddButton<CompactListQuery>("Отмена \u274c");

      user = GetParameterUser(parameters);

      Text.Italic("Действительно отписаться от ")
         .InlineMention(user)
         .Italic("?");
   }
}
