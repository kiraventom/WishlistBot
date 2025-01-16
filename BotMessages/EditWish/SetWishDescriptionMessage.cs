using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishDescriptionMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (!string.IsNullOrEmpty(user.CurrentWish.Description))
         Keyboard.AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Description));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      if (user.CurrentWish.Description is null)
      {
         Text.Verbatim("Укажите подробное описание виша:");
      }
      else
      {
         Text
            .Bold("Текущее описание виша:")
            .LineBreak().Monospace(user.CurrentWish.Description)
            .LineBreak()
            .LineBreak().Verbatim("Укажите новое описание или удалите текущее:");
      }

      user.BotState = BotState.ListenForWishDescription;

      return Task.CompletedTask;
   }
}
