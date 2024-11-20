using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class EditListMessage : BotMessage
{
   public EditListMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      // TODO: Allow pages scrolling
      const int wishesPerPage = 5;

      for (int i = 0; i < wishesPerPage; ++i)
      {
         if (i >= user.Wishes.Count)
            break;

         var wish = user.Wishes[i];

         const string pencilEmoji = " \u270f\ufe0f";
         
         Keyboard.AddButton<EditWishQuery>(
               wish.Name + pencilEmoji, 
               new QueryParameter(QueryParameterType.SetCurrentWishTo, (byte)i), 
               QueryParameter.ReturnToEditList);

         Keyboard.NewRow();
      }

      Keyboard.AddButton("@prev_page", "\u2b05\ufe0f");
      
      if (parameters.Peek(QueryParameterType.ReturnToCompactList))
         Keyboard.AddButton<CompactListMyWishesQuery>("Назад");
      else if (parameters.Peek(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListMyWishesQuery>("Назад");

      Keyboard.AddButton("@next_page", "\u27a1\ufe0f");

      Text = "Ваши виши:";

      user.BotState = BotState.EditingList;
   }
}
