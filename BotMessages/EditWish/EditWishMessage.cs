using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo, QueryParameterType.ClearWishProperty)]
[ChildMessage(typeof(FullListMessage))]
// TODO Separate to NewWish and EditWish
public class EditWishMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
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
         parameters.Peek(QueryParameterType.SetWishTo, out var wishId);

         if (user.CurrentWish is null)
         {
            var wishToClone = user.Wishes.FirstOrDefault(w => w.Id == wishId);
            if (wishToClone is null)
            {
               throw new NotSupportedException($"Can't find wish {wishId} to clone");
            }

            user.CurrentWish = wishToClone.Clone(GenerateWishId());
         }

         Keyboard.AddButton<ConfirmDeleteWishQuery>(new QueryParameter(QueryParameterType.SetWishTo, wishId));
         Keyboard.NewRow();
         Keyboard.AddButton<FinishEditWishQuery>(new QueryParameter(QueryParameterType.SetWishTo, wishId));
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

      if (links.Any())
      {
         Text.LineBreak().Bold("Ссылки: ");
         for (var i = 0; i < links.Count; ++i)
         {
            var link = links[i];
            Text.InlineUrl(link);
            if (i < links.Count - 1)
               Text.Verbatim(", ");
         }
      }

      PhotoFileId = wish.FileId;

      return Task.CompletedTask;
   }
}
