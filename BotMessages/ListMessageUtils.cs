using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages;

#pragma warning disable CS1998
public static class ListMessageUtils
{
   public static int ItemsPerPage = 5;

   public static void AddListControls<TListQuery, TParentQuery>(BotKeyboard keyboard, QueryParameterCollection parameters, int totalCount, Action<int, int> addButtonAt)
      where TListQuery : IQuery, new() where TParentQuery : IQuery, new()
   {
      var pageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndexValue))
         pageIndex = (int)pageIndexValue;

      if (totalCount == 0)
         keyboard.AddButton<TParentQuery>();

      var pagesCount = (int)Math.Ceiling((double)totalCount / ItemsPerPage);

      // Can happen if the only item on the last page was removed
      if (pageIndex >= pagesCount)
         pageIndex = pagesCount - 1;

      for (var itemOnPageIndex = 0; itemOnPageIndex < ItemsPerPage; ++itemOnPageIndex)
      {
         var itemIndex = pageIndex * ItemsPerPage + itemOnPageIndex;
         if (itemIndex >= totalCount)
            break;

         addButtonAt(itemIndex, pageIndex);
         keyboard.NewRow();
      }

      if (pageIndex > 0)
         keyboard.AddButton<TListQuery>($"\u2b05\ufe0f {pageIndex - 1}", new QueryParameter(QueryParameterType.SetListPageTo, pageIndex - 1));

      keyboard.AddButton<TParentQuery>();

      if (pageIndex < pagesCount - 1)
         keyboard.AddButton<TListQuery>($"{pageIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, pageIndex + 1));
   }
}
