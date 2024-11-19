using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages.EditingWish;

public class EditingWishMessage : BotMessage
{
   public EditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, IReadOnlyCollection<string> parameters = null)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>()
         .AddButton<SetWishDescriptionQuery>()
         .NewRow()
         .AddButton<SetWishMediaQuery>()
         .AddButton<SetWishLinksQuery>()
         .NewRow()
         .AddButton<FinishEditingWishQuery>()
         .AddButton<CancelEditingWishQuery>();


      var wish = user.CurrentWish;

      if (HasParameter(parameters, "clearMedia"))
         wish.FileId = null;

      var name = wish.Name;
      var description = wish.Description ?? "<не указано>";
      var links = wish.Links.Count; // TODO: Replace with inline links

      Text = $"Редактирование виша\n\nНазвание: {name}\nОписание: {description}\nСсылки: {links}";

      PhotoFileId = wish.FileId;

      user.BotState = BotState.EditingWish;
   }
}
