using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages;

public class CompactListMyWishesMessage : BotMessage
{
   public CompactListMyWishesMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<EditListQuery>(QueryParameter.ReturnToCompactList)
         .NewRow()
         .AddButton<FullListMyWishesQuery>()
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      parameters.Pop(QueryParameterType.ReturnToCompactList);

      var stringBuilder = new StringBuilder();
      stringBuilder.AppendLine("Краткий список ваших вишей:");
      for (int i = 0; i < user.Wishes.Count; ++i)
      {
         var wish = user.Wishes[i];
         stringBuilder.Append(i + 1).Append(". ");
         stringBuilder.Append(wish.Name);

         if (wish.FileId is not null)
            stringBuilder.Append(" \U0001f5bc\U0000fe0f");

         if (wish.Links.Any())
            stringBuilder.Append(" \U0001f310");

         stringBuilder.AppendLine();
      }

      Text = stringBuilder.ToString();

      user.BotState = BotState.CompactListMyWishes;
   }
}
