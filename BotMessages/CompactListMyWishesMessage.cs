using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages;

public class CompactListMyWishesMessage : BotMessage
{
   public CompactListMyWishesMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton("@edit", "Редактировать список")
         .NewRow()
         .AddButton<FullListMyWishesQuery>()
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      var stringBuilder = new StringBuilder();
      stringBuilder.AppendLine("Краткий список ваших вишей:");
      for (int i = 0; i < user.Wishes.Count; ++i)
      {
         var wish = user.Wishes[i];
         stringBuilder.Append(i + 1).Append(". ");
         stringBuilder.Append(wish.Name);
         // TODO: Links, images emojis
         stringBuilder.AppendLine();
      }

      Text = stringBuilder.ToString();

      user.BotState = BotState.CompactListMyWishes;
   }
}
