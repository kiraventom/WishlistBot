using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReadOnly, QueryParameterType.ReturnToSubscriber)]
public class CompactListMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      user = GetParameterUser(parameters);

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

      return Task.CompletedTask;
   }
}
