using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

// TODO Combine common code with EditWish
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
