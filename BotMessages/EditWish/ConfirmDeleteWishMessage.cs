using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class ConfirmDeleteWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<DeleteWishQuery>()
         .NewRow()
         .AddButton<EditWishQuery>("Отмена \u274c");

      if (user.CurrentWish.ClaimerId != 0)
      {
         Text.ItalicBold("\u203c\ufe0f Будьте осторожны! Кто-то забронировал этот виш! \u203c\ufe0f").LineBreak().LineBreak();
      }

      Text.Italic("Действительно удалить виш \"")
         .Monospace(user.CurrentWish.Name)
         .Italic("\"")
         .ItalicBold(" навсегда")
         .Italic("?");

      return Task.CompletedTask;
   }
}
