using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class InvalidMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Text.Bold("Некорректное действие");

      return Task.CompletedTask;
   }
}
