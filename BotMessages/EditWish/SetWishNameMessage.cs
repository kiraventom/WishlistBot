using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ForceNewWish)]
public class SetWishNameMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<CancelEditWishQuery>();

      var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);

      if (forceNewWish || user.CurrentWish is null)
      {
         user.CurrentWish = new Wish();
         Text.Verbatim("Укажите краткое название виша:");
      }
      else
      {
         Text
            .Bold("Текущее название виша: ")
            .Monospace(user.CurrentWish.Name)
            .LineBreak()
            .LineBreak().Verbatim("Укажите новое название виша:");
      }

      user.BotState = BotState.ListenForWishName;
   }
}
