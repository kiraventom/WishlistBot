using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages;

public abstract class UserBotMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
   protected IEnumerable<BotUser> Users => usersDb.Values.Values;

   protected BotUser GetParameterUser(QueryParameterCollection parameters)
   {
      parameters.Peek(QueryParameterType.SetUserTo, out var userId);
      if (usersDb.Values.TryGetValue(userId, out var user))
         return user;

      try
      {
         throw new NotSupportedException($"Can't set user to [{userId}], users db does not contain user with this ID");
      }
      catch (Exception e)
      {
         Logger.Error(e.ToString());
         throw;
      }
   }
}
