using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

public class SetWishNameMessage : BotMessage
{
   public SetWishNameMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<CancelEditingWishQuery>();

      var stringBuilder = new StringBuilder();

      var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);
      Logger.Debug("forceNewWish: {forceNewWish}", forceNewWish);
      Logger.Debug("parameters: {parameters}", parameters.ToString());

      if (forceNewWish || user.CurrentWish is null)
      {
         user.CurrentWish = new Wish();
         stringBuilder.AppendLine("Укажите краткое название виша:");
      }
      else
      {
         // TODO: Name in Markdown monospace
         stringBuilder
            .Append("Текущее название виша: ")
            .AppendLine(user.CurrentWish.Name)
            .AppendLine()
            .AppendLine("Укажите новое название виша:");
      }

      Text = stringBuilder.ToString();

      user.BotState = BotState.SettingWishName;
   }
}
