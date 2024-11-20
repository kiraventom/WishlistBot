using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages.EditingWish;

public class ConfirmWishDeletionMessage : BotMessage
{
   public ConfirmWishDeletionMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<ConfirmWishDeletionQuery>()
         .NewRow()
         .AddButton<EditWishQuery>("Отмена \u274c");

      Text = $"Действительно удалить виш \"{user.CurrentWish.Name}\" навсегда?";

      user.BotState = BotState.DeletingWish;
   }
}
