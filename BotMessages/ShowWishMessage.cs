using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetWishTo)]
[ChildMessage(typeof(FullListMessage))]
public class ShowWishMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<FullListQuery>("Назад", QueryParameter.ReadOnly);

      user = GetUser(user, parameters);

      parameters.Pop(QueryParameterType.SetWishTo, out var wishId);

      var wish = user.Wishes.FirstOrDefault(w => w.Id == wishId);
      if (wish is null)
      {
         throw new NotSupportedException($"Can't find wish {wishId} to show");
      }

      var name = wish.Name;
      var description = wish.Description;
      var links = wish.Links;

      Text.Bold("Название: ").Monospace(name);

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

      return Task.CompletedTask;
   }
}
