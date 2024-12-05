using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class CompactListMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (usersDb.Values.TryGetValue(userId, out var user0))
            user = user0;
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);

      if (!isReadOnly)
         Keyboard.AddButton<SetWishNameQuery>("Добавить виш", QueryParameter.ForceNewWish);

      if (user.Wishes.Count != 0)
         Keyboard.AddButton<FullListQuery>();

      Keyboard.NewRow();

      if (isReadOnly)
      {
         if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
            Keyboard.AddButton<SubscriberQuery>("К подписчику");
         else
            Keyboard.AddButton<SubscriptionQuery>("К подписке");
      }
      else
      {
         Keyboard.AddButton<MainMenuQuery>("В главное меню");
      }

      if (isReadOnly)
         Text.Bold("Краткий список вишей ")
            .InlineMention(user)
            .Bold(":");
      else
         Text.Bold("Краткий список ваших вишей:");

      for (var i = 0; i < user.Wishes.Count; ++i)
      {
         var wish = user.Wishes[i];
         Text.LineBreak().Bold($"{i + 1}. ").Monospace(wish.Name);

         if (!string.IsNullOrEmpty(wish.Description))
            Text.Verbatim(" \U0001f4ac"); // speech bubble

         if (wish.FileId is not null)
            Text.Verbatim(" \U0001f5bc\ufe0f"); // picture

         if (wish.Links.Any())
            Text.Verbatim(" \U0001f310"); // globe
      }
   }
}
