using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList)]
public class CancelEditWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (parameters.Pop(QueryParameterType.ReturnToFullList))
      {
         Text.Italic("Редактирование виша отменено");
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      }
      else
      {
         Text.Italic("Создание виша отменено");
         Keyboard.AddButton<SetWishNameQuery>("Добавить другой виш")
            .NewRow()
            .AddButton<CompactListQuery>("Назад к моим вишам");
      }

      user.CurrentWish = null;

      return Task.CompletedTask;
   }
}
