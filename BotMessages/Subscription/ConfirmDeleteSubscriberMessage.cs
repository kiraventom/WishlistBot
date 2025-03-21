using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(SubscriberMessage))]
public class ConfirmDeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<DeleteSubscriberQuery>()
         .NewRow()
         .AddButton<SubscriberQuery>("Отмена \u274c");

      user = GetUser(user, parameters);

      Text.Italic("Действительно удалить ")
         .InlineMention(user)
         .Italic(" из списка подписчиков?")
         .LineBreak()
         .ItalicBold("После этого ")
         .InlineMention(user)
         .ItalicBold(" больше не сможет видеть ваши виши.");

      return Task.CompletedTask;
   }
}
