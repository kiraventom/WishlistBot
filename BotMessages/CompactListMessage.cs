using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages;

public class CompactListMessage : BotMessage
{
   public CompactListMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (user.Wishes.Count != 0)
         Keyboard.AddButton<FullListQuery>();

        Keyboard
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text.Bold("Краткий список ваших вишей:");
      for (int i = 0; i < user.Wishes.Count; ++i)
      {
         var wish = user.Wishes[i];
         Text.LineBreak().Bold($"{i + 1}. ").Monospace(wish.Name);

         if (wish.FileId is not null)
            Text.Verbatim(" \U0001f5bc\U0000fe0f");

         if (wish.Links.Any())
            Text.Verbatim(" \U0001f310");
      }

      user.BotState = BotState.CompactList;
   }
}
