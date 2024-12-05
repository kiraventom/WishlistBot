using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class ConfirmDeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<DeleteSubscriberQuery>()
         .NewRow()
         .AddButton<SubscriberQuery>("Отмена \u274c");

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (usersDb.Values.TryGetValue(userId, out var user0))
            user = user0;
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      Text.Italic("Действительно удалить ")
         .InlineMention(user)
         .Italic(" из списка подписчиков?")
         .LineBreak()
         .ItalicBold("После этого ")
         .InlineMention(user)
         .ItalicBold(" больше не сможет видеть ваши виши.");
   }
}
