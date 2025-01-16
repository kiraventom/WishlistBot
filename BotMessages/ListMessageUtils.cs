using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

public static class ListMessageUtils
{
   public const int ItemsPerPage = 5;

   public static void AddListControls<TListQuery, TParentQuery>(BotKeyboard keyboard, QueryParameterCollection parameters, int totalCount, Action<int, int> addButtonAt)
      where TListQuery : IQuery, new() where TParentQuery : IQuery, new()
   {
      var pageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndexValue))
         pageIndex = (int)pageIndexValue;

      if (totalCount == 0)
      {
         keyboard.AddButton<TParentQuery>();
         return;
      }

      var pagesCount = (int)Math.Ceiling((double)totalCount / ItemsPerPage);

      // Can happen if the only item on the last page was removed
      if (pagesCount != 0 && pageIndex >= pagesCount)
         pageIndex = pagesCount - 1;

      for (var itemOnPageIndex = 0; itemOnPageIndex < ItemsPerPage; ++itemOnPageIndex)
      {
         var itemIndex = pageIndex * ItemsPerPage + itemOnPageIndex;
         if (itemIndex >= totalCount)
            break;

         addButtonAt(itemIndex, pageIndex);
         keyboard.NewRow();
      }

      var prevPageIndex = pageIndex - 1;
      var nextPageIndex = pageIndex + 1;

      if (pageIndex > 0)
         keyboard.AddButton<TListQuery>($"\u2b05\ufe0f {prevPageIndex + 1}", new QueryParameter(QueryParameterType.SetListPageTo, prevPageIndex));

      keyboard.AddButton<TParentQuery>("Назад");

      if (pageIndex < pagesCount - 1)
         keyboard.AddButton<TListQuery>($"{nextPageIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, nextPageIndex));
   }
}
