using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditingWish;

public class ConfirmDeleteWishMessage : BotMessage
{
   public ConfirmDeleteWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<ConfirmDeleteWishQuery>()
         .NewRow()
         .AddButton<EditWishQuery>("Отмена \u274c");

      Text.Italic("Действительно удалить виш \"")
         .Monospace(user.CurrentWish.Name)
         .Italic("\"")
         .ItalicBold(" навсегда")
         .Italic("?");

      user.BotState = BotState.DeletingWish;
   }
}
