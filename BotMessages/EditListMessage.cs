using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class EditListMessage : BotMessage
{
   public EditListMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, params QueryParameter[] parameters)
   {
      Keyboard = new BotKeyboard();

      // TODO: Allow pages scrolling
      const int wishesPerPage = 5;

      for (int i = 0; i < wishesPerPage; ++i)
      {
         if (i >= user.Wishes.Count)
            break;

         var wish = user.Wishes[i];
         Keyboard.AddButton<EditWishQuery>(wish.Name, new QueryParameter(QueryParameterType.SetCurrentWishTo, (byte)i), QueryParameter.ReturnToEditList);
         Keyboard.AddButton("@delete", "\U0001f5d1\U0000fe0f");
         Keyboard.NewRow();
      }

      Keyboard.AddButton("@prev_page", "\u2b05\ufe0f");
      Keyboard.AddButton("@next_page", "\u27a1\ufe0f");

      Text = "Ваши виши:";

      user.BotState = BotState.EditingList;
   }
}
