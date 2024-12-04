using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class ShowWishMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public ShowWishMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<FullListQuery>("Назад");

      // TODO: This code appears in three different classes. Fix
      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      parameters.Pop(QueryParameterType.SetCurrentWishTo, out var setWishIndex);

      var wish = user.Wishes[(int)setWishIndex];
      var name = wish.Name;
      var description = wish.Description;
      var links = wish.Links;

      Text.Bold("Название: ").Monospace(name);
      
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
   }
}
