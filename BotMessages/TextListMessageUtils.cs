using WishlistBot.Keyboard;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages;

public static class TextListMessageUtils
{
   public const int ItemsPerPage = 15;

   public static int GetPagesCount(int totalCount) => (int)Math.Ceiling((double)totalCount / ItemsPerPage);

   public static void AddListControls<TListQuery, TParentQuery>(MessageText text, BotKeyboard keyboard, QueryParameterCollection parameters, int totalCount, IListPage listPosition, Action<int> addLineAt, string buttonCaption = null)
      where TListQuery : IQuery, new() where TParentQuery : IQuery, new()
      {
          buttonCaption = buttonCaption is null ? string.Empty : $"{buttonCaption} ";

          keyboard.NewRow();

          var pageIndex = listPosition.Page;
          if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndexValue))
              pageIndex = (int)pageIndexValue;

          if (totalCount == 0)
          {
              keyboard.AddButton<TParentQuery>("Назад");
              return;
          }

          var pagesCount = GetPagesCount(totalCount);

          // Page can be out of range: the only item on the last page was removed,
          // or the page came from a parameter of another list (see SetListPageTo)
          pageIndex = Math.Clamp(pageIndex, 0, pagesCount - 1);

          listPosition.Page = pageIndex;

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
              keyboard.AddButton<TListQuery>($"\u2b05\ufe0f {buttonCaption}{prevPageIndex + 1}", new QueryParameter(QueryParameterType.SetListPageTo, prevPageIndex));

          keyboard.AddButton<TParentQuery>("Назад");

          if (pageIndex < pagesCount - 1)
              keyboard.AddButton<TListQuery>($"{buttonCaption}{nextPageIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, nextPageIndex));
      }
}

