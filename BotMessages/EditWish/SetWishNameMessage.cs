using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages.EditWish;

public class SetWishNameMessage : BotMessage
{
   public SetWishNameMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<CancelEditWishQuery>();

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

      user.BotState = BotState.SettingWishName;
   }
}
