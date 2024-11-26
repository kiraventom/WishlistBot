using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class FullListMessage : BotMessage
{
   public FullListMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      int currentPageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndex))
         currentPageIndex = pageIndex;

      const int wishesPerPage = 5;

      int pagesCount = (int)Math.Ceiling((double)user.Wishes.Count / wishesPerPage);

      if (pagesCount == 0)
      {
         Text.Bold("Список пуст");
         Keyboard.AddButton<CompactListQuery>("Назад");
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

         const string pencilEmoji = "\u270f\ufe0f ";
         
         Keyboard.AddButton<EditWishQuery>(
               pencilEmoji + wish.Name,
               new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex), 
               new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex),
               QueryParameter.ReturnToFullList);

         Keyboard.NewRow();
      }

      if (currentPageIndex > 0)
         Keyboard.AddButton<FullListQuery>("\u2b05\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex - 1));
      
      Keyboard.AddButton<CompactListQuery>("Назад");

      if (currentPageIndex < pagesCount - 1)
         Keyboard.AddButton<FullListQuery>("\u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex + 1));

      Text.Bold("Ваши виши")
         .LineBreak().Bold($"Страница {currentPageIndex + 1} из {pagesCount}");

      user.BotState = BotState.EditingList;
   }
}
