using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class ConfirmDeleteSubscriberMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public ConfirmDeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<DeleteSubscriberQuery>()
         .NewRow()
         .AddButton<SubscriberQuery>("Отмена \u274c");

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
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
