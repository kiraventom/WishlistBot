using Serilog;
using WishlistBot.Queries;
using WishlistBot.Keyboard;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages.EditingWish;

public class EditingWishMessage : BotMessage
{
   public EditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, params QueryParameter[] parameters)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>(parameters: parameters)
         .AddButton<SetWishDescriptionQuery>(parameters: parameters)
         .NewRow()
         .AddButton<SetWishMediaQuery>(parameters: parameters)
         .AddButton<SetWishLinksQuery>(parameters: parameters)
         .NewRow()
         .AddButton<FinishEditingWishQuery>(parameters: parameters)
         .AddButton<CancelEditingWishQuery>(parameters: parameters);

      var setWishIndex = GetParameter(parameters, QueryParameterType.SetCurrentWishTo);

      if (user.CurrentWish is null && setWishIndex.HasValue)
      {
         Logger.Debug("GetParameter setWishIndex={wishIndex}", setWishIndex.Value);
         user.CurrentWish = user.Wishes[setWishIndex.Value];
      }

      var wish = user.CurrentWish;

      if (HasParameter(parameters, QueryParameterType.ClearWishMedia))
         wish.FileId = null;

      var name = wish.Name;
      var description = wish.Description ?? "<не указано>";
      var links = wish.Links.Count; // TODO: Replace with inline links

      Text = $"Редактирование виша\n\nНазвание: {name}\nОписание: {description}\nСсылки: {links}";

      PhotoFileId = wish.FileId;

      user.BotState = BotState.EditingWish;
   }
}
