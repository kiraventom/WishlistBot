using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

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
         .NewRow();

      if (parameters.Peek(QueryParameterType.ReturnToEditList))
      {
         Keyboard.AddButton<DeleteWishQuery>();
         Keyboard.NewRow();
      }

      if (parameters.Pop(QueryParameterType.SetCurrentWishTo, out var setWishIndex))
      {
         user.CurrentWish = user.Wishes[setWishIndex];
      }

      Keyboard.AddButton<FinishEditingWishQuery>();
      Keyboard.AddButton<CancelEditingWishQuery>();

      var wish = user.CurrentWish;

      if (parameters.Pop(QueryParameterType.ClearWishProperty, out var propertyTypeValue))
      {
         var propertyType = (WishPropertyType)propertyTypeValue;
         switch (propertyType)
         {
            case WishPropertyType.Description:
               wish.Description = null;
               break;
            case WishPropertyType.Media:
               wish.FileId = null;
               break;
            case WishPropertyType.Links:
               wish.Links.Clear();
               break;
         }
      }

      var name = wish.Name;
      var description = wish.Description ?? "<не указано>";
      var links = wish.Links.Count; // TODO: Replace with inline links

      Text = $"Редактирование виша\n\nНазвание: {name}\nОписание: {description}\nСсылки: {links}";

      PhotoFileId = wish.FileId;

      user.BotState = BotState.EditingWish;
   }
}
