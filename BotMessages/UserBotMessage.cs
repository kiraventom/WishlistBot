using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetUserTo)]
public abstract class UserBotMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
   protected IEnumerable<BotUser> Users => usersDb.Values.Values;

   protected BotUser GetUser(BotUser sender, QueryParameterCollection parameters)
   {
      parameters.Peek(QueryParameterType.SetUserTo, out var userId);
      return usersDb.Values.GetValueOrDefault(userId, sender);
   }

   protected long GenerateWishId()
   {
      var ids = Users.SelectMany(u => u.Wishes).Select(w => w.Id).ToList();
      return DatabaseUtils.GenerateId(ids);
   }
}
