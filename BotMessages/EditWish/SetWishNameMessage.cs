using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ForceNewWish)]
public class SetWishNameMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);

      if (forceNewWish || user.CurrentWish is null)
      {
         user.CurrentWish = new Wish()
         {
            Id = GenerateWishId()
         };
         Text.Verbatim("Укажите краткое название виша:");
         Keyboard.AddButton<CancelEditWishQuery>();
      }
      else
      {
         Text
            .Bold("Текущее название виша: ")
            .Monospace(user.CurrentWish.Name)
            .LineBreak()
            .LineBreak().Verbatim("Укажите новое название виша:");

         Keyboard.AddButton<EditWishQuery>("Отмена");
      }

      user.BotState = BotState.ListenForWishName;

      return Task.CompletedTask;
   }
}
