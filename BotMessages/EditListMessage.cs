using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class EditListMessage : BotMessage
{
   public EditListMessage(ILogger logger) : base(logger)
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
         if (parameters.Peek(QueryParameterType.ReturnToCompactList))
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
               QueryParameter.ReturnToEditList);

         Keyboard.NewRow();
      }

      if (currentPageIndex > 0)
         Keyboard.AddButton<EditListQuery>("\u2b05\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex - 1));
      
      if (parameters.Peek(QueryParameterType.ReturnToCompactList))
         Keyboard.AddButton<CompactListQuery>("Назад");

      if (currentPageIndex < pagesCount - 1)
         Keyboard.AddButton<EditListQuery>("\u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex + 1));

      Text.Bold("Ваши виши")
         .LineBreak().Bold($"Страница {currentPageIndex + 1} из {pagesCount}");

      user.BotState = BotState.EditingList;
   }
}
