using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetCurrentWishTo, QueryParameterType.ClearWishProperty)]
public class EditWishMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SetWishNameQuery>()
         .AddButton<SetWishDescriptionQuery>()
         .NewRow()
         .AddButton<SetWishMediaQuery>()
         .AddButton<SetWishLinksQuery>()
         .NewRow();

      if (parameters.Peek(QueryParameterType.ReturnToFullList))
      {
         parameters.Peek(QueryParameterType.SetCurrentWishTo, out var setWishIndex);

         user.CurrentWish ??= user.Wishes[(int)setWishIndex].Clone();

         Keyboard.AddButton<ConfirmDeleteWishQuery>(new QueryParameter(QueryParameterType.SetCurrentWishTo, setWishIndex));
         Keyboard.NewRow();
         Keyboard.AddButton<FinishEditWishQuery>(new QueryParameter(QueryParameterType.SetCurrentWishTo, setWishIndex));
      }
      else
      {
         Keyboard.AddButton<FinishEditWishQuery>();
      }

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
         for (var i = 0; i < links.Count; ++i)
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
   }
}
