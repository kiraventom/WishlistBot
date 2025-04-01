using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

public class InvalidMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Text.Bold("Что-то пошло не так :(")
         .LineBreak().LineBreak()
         .Italic("Попробуйте отправить команду /start и выполнить действие ещё раз.")
         .LineBreak().Italic("Если проблема повторится, сообщите об этом разработчику");

      return Task.CompletedTask;
   }
}
