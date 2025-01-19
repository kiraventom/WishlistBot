using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReadOnly, QueryParameterType.ReturnToSubscriber)]
public class CompactListMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var sender = user;
      user = GetUser(user, parameters);

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
         Text.LineBreak().Bold($"{i + 1}. ");

         // If user isn't looking at its own wishes
         if (sender.SenderId != user.SenderId && wish.ClaimerId != 0)
         {
            Text.Bold("[БРОНЬ] ").Strikethrough(wish.Name);
         }
         else
         {
            Text.Verbatim(wish.Name);
         }

         if (wish.PriceRange != Price.NotSet)
         {
            Text.Verbatim(" [").Bold(MessageTextUtils.PriceToShortString(wish.PriceRange)).Verbatim("] ");
         }

         if (!string.IsNullOrEmpty(wish.Description))
            Text.Verbatim(" \U0001f4ac"); // speech bubble

         if (wish.FileId is not null)
            Text.Verbatim(" \U0001f5bc\ufe0f"); // picture

         if (wish.Links.Any())
         {
            var firstLink = wish.Links.First();
            Text.InlineUrl(" \U0001f310", firstLink); // globe
         }
      }

      return Task.CompletedTask;
   }
}
