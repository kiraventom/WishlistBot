using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages.EditingWish;

public class EditingWishMessage : BotMessage
{
   public EditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>()
         .AddButton<SetWishDescriptionQuery>()
         .NewRow()
         .AddButton<SetWishMediaQuery>()
         .AddButton<SetWishLinksQuery>()
         .NewRow()
         .AddButton<FinishEditingWishQuery>()
         .AddButton<CancelEditingWishQuery>();

      Logger.Debug("EditingWish: {parameters}", parameters.ToString());

      if (parameters.Pop(QueryParameterType.SetCurrentWishTo, out var setWishIndex))
         user.CurrentWish = user.Wishes[setWishIndex];

      Logger.Debug("EditingWish: {parameters}", parameters.ToString());

      var wish = user.CurrentWish;

      if (parameters.Pop(QueryParameterType.ClearWishMedia))
         wish.FileId = null;

      var name = wish.Name;
      var description = wish.Description ?? "<не указано>";
      var links = wish.Links.Count; // TODO: Replace with inline links

      Text = $"Редактирование виша\n\nНазвание: {name}\nОписание: {description}\nСсылки: {links}";

      PhotoFileId = wish.FileId;

      user.BotState = BotState.EditingWish;
   }
}
