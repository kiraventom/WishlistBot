using WishlistBot.Keyboard;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

/// <summary>Кусок списка, который попал на текущую страницу.</summary>
public readonly record struct ListPage(int PageIndex, int FirstIndex, int Count)
{
    public int LastIndex => FirstIndex + Count - 1;
}

public static class TextListMessageUtils
{
    public const int ItemsPerPage = 15;

    public static int GetPagesCount(int totalCount) => (int)Math.Ceiling((double)totalCount / ItemsPerPage);

    /// <summary>
    /// Страница, на которой лежит элемент с указанным индексом. Индекс -1 значит «не нашли»
    /// (такое бывает у ссылок из уведомлений) — ведём на первую страницу, а не в минус.
    /// </summary>
    public static int GetPageIndex(int itemIndex) => Math.Max(itemIndex, 0) / ItemsPerPage;

    /// <summary>
    /// Кнопки листания и «Назад» для списка. Текст страницы собирает сам экран: с Rich Markdown
    /// список — это таблица или набор строк, а не «строка + перевод строки», как раньше.
    /// </summary>
    public static ListPage AddListControls<TListQuery, TParentQuery>(
        BotKeyboard keyboard,
        QueryParameterCollection parameters,
        int totalCount,
        IListPage listPosition,
        string buttonCaption = null)
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
            return new ListPage(0, 0, 0);
        }

        var pagesCount = GetPagesCount(totalCount);

        // Page can be out of range: the only item on the last page was removed,
        // or the page came from a parameter of another list (see SetListPageTo)
        pageIndex = Math.Clamp(pageIndex, 0, pagesCount - 1);

        listPosition.Page = pageIndex;

        var firstIndex = pageIndex * ItemsPerPage;
        var count = Math.Min(ItemsPerPage, totalCount - firstIndex);

        var prevPageIndex = pageIndex - 1;
        var nextPageIndex = pageIndex + 1;

        if (pageIndex > 0)
            keyboard.AddButton<TListQuery>($"\u2b05\ufe0f {buttonCaption}{prevPageIndex + 1}", new QueryParameter(QueryParameterType.SetListPageTo, prevPageIndex));

        keyboard.AddButton<TParentQuery>("Назад");

        if (pageIndex < pagesCount - 1)
            keyboard.AddButton<TListQuery>($"{buttonCaption}{nextPageIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, nextPageIndex));

        return new ListPage(pageIndex, firstIndex, count);
    }
}
