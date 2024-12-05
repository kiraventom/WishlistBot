using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class ConfirmDeleteWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<DeleteWishQuery>()
         .NewRow()
         .AddButton<EditWishQuery>("Отмена \u274c");

      Text.Italic("Действительно удалить виш \"")
         .Monospace(user.CurrentWish.Name)
         .Italic("\"")
         .ItalicBold(" навсегда")
         .Italic("?");

      return Task.CompletedTask;
   }
}
