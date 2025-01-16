using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishMediaMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (user.CurrentWish.FileId is not null)
         Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      PhotoFileId = user.CurrentWish.FileId;

      Text.Verbatim(PhotoFileId is not null ? "Пришлите новое фото или удалите текущее" : "Пришлите фото виша:");

      user.BotState = BotState.ListenForWishMedia;

      return Task.CompletedTask;
   }
}
