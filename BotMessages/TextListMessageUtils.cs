using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages;

public static class TextListMessageUtils
{
   public const int ItemsPerPage = 15;

   public static void AddListControls<TListQuery, TParentQuery>(MessageText text, BotKeyboard keyboard, QueryParameterCollection parameters, int totalCount, ListPositionModel listPosition, Action<int> addLineAt)
      where TListQuery : IQuery, new() where TParentQuery : IQuery, new()
      {
          keyboard.NewRow();

          var pageIndex = listPosition.Page;
          if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndexValue))
              pageIndex = (int)pageIndexValue;

          if (totalCount == 0)
          {
              keyboard.AddButton<TParentQuery>("Назад");
              return;
          }

          var pagesCount = (int)Math.Ceiling((double)totalCount / ItemsPerPage);

          // Can happen if the only item on the last page was removed
          if (pagesCount != 0 && pageIndex >= pagesCount)
              pageIndex = pagesCount - 1;

          Log.Logger.Warning("listPos.Page before: {0}", listPosition.Page);
          listPosition.Page = pageIndex;
          Log.Logger.Warning("listPos.Page after: {0}", listPosition.Page);

          for (var itemOnPageIndex = 0; itemOnPageIndex < ItemsPerPage; ++itemOnPageIndex)
          {
              var itemIndex = pageIndex * ItemsPerPage + itemOnPageIndex;
              if (itemIndex >= totalCount)
                  break;

              addLineAt(itemIndex);
              text.LineBreak();
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

