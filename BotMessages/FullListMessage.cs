using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class FullListMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public FullListMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      // Needs to be cleared if returned from ShowWish.
      parameters.Pop(QueryParameterType.ReturnToFullList); 

      var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);
      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      int currentPageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndex))
         currentPageIndex = (int)pageIndex;

      const int wishesPerPage = 5;

      int pagesCount = (int)Math.Ceiling((double)user.Wishes.Count / wishesPerPage);

      if (pagesCount == 0)
      {
         Text.Bold("Список пуст");
         Keyboard.AddButton<CompactListQuery>("Назад");
         return;
      }

      // Can happen if the only wish on the last page was deleted
      if (currentPageIndex >= pagesCount)
         currentPageIndex = pagesCount - 1;

      for (int i = 0; i < wishesPerPage; ++i)
      {
         var wishIndex = currentPageIndex * wishesPerPage + i;
         if (wishIndex >= user.Wishes.Count)
            break;

         var wish = user.Wishes[wishIndex];

         // TODO This is ugly. Maybe divide method to two different Inits for readonly and non-readonly versions?
         if (isReadOnly)
         {
            const string eyeEmoji = "\U0001f441\U0000fe0f ";

            Keyboard.AddButton<ShowWishQuery>(
                  eyeEmoji + wish.Name,
                  new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex), 
                  new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex),
                  QueryParameter.ReturnToFullList);
         }
         else
         {
            const string pencilEmoji = "\u270f\ufe0f ";

            Keyboard.AddButton<EditWishQuery>(
                  pencilEmoji + wish.Name,
                  new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex), 
                  new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex),
                  QueryParameter.ReturnToFullList);
         }

         Keyboard.NewRow();
      }

      if (currentPageIndex > 0)
         Keyboard.AddButton<FullListQuery>("\u2b05\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex - 1));

      Keyboard.AddButton<CompactListQuery>("Назад");

      if (currentPageIndex < pagesCount - 1)
         Keyboard.AddButton<FullListQuery>("\u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex + 1));

      if (isReadOnly)
         Text.Bold("Виши ")
            .InlineMention(user)
            .Bold(":");
      else
         Text.Bold("Ваши виши:");


      Text.LineBreak().Bold($"Страница {currentPageIndex + 1} из {pagesCount}");
   }
}
