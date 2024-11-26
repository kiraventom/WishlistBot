using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class EditWishMessage : BotMessage
{
   public EditWishMessage(ILogger logger) : base(logger)
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

      // TODO: Also check if in editing mode
      if (parameters.Peek(QueryParameterType.ReturnToFullList))
      {
         Keyboard.AddButton<DeleteWishQuery>();
         Keyboard.NewRow();
      }

      if (parameters.Pop(QueryParameterType.SetCurrentWishTo, out var setWishIndex))
      {
         user.CurrentWish = user.Wishes[setWishIndex];
      }

      Keyboard.AddButton<FinishEditWishQuery>();
      Keyboard.AddButton<CancelEditWishQuery>();

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
      var description = wish.Description;
      var links = wish.Links;

      Text.Italic("Редактирование виша")
         .LineBreak()
         .LineBreak().Bold("Название: ").Monospace(name);
      
      if (description is not null)
         Text.LineBreak().Bold("Описание: ").LineBreak().ExpandableQuote(description);

      if (links.Count > 1)
      {
         Text.LineBreak().Bold("Ссылки: ");
         for (int i = 0; i < links.Count; ++i)
         {
            var link = links[i];
            Text.InlineUrl($"Ссылка {i + 1}", link);
            if (i < links.Count - 1)
               Text.Verbatim(", ");
         }
      }
      else if (links.Count == 1)
      {
         Text.LineBreak().InlineUrl("Ссылка", links.First());
      }


      PhotoFileId = wish.FileId;

      user.BotState = BotState.EditWish;
   }
}
