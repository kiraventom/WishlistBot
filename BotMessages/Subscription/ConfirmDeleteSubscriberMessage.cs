using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class ConfirmDeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<DeleteSubscriberQuery>()
         .NewRow()
         .AddButton<SubscriberQuery>("Отмена \u274c");

      user = GetParameterUser(parameters);

      Text.Italic("Действительно удалить ")
         .InlineMention(user)
         .Italic(" из списка подписчиков?")
         .LineBreak()
         .ItalicBold("После этого ")
         .InlineMention(user)
         .ItalicBold(" больше не сможет видеть ваши виши.");
   }
}
