using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class ConfirmDeleteWishMessage : BotMessage
{
   public ConfirmDeleteWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<DeleteWishQuery>()
         .NewRow()
         .AddButton<EditWishQuery>("Отмена \u274c");

      Text.Italic("Действительно удалить виш \"")
         .Monospace(user.CurrentWish.Name)
         .Italic("\"")
         .ItalicBold(" навсегда")
         .Italic("?");
   }
}
