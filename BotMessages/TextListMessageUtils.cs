using WishlistBot.Keyboard;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages;

/// <summary>Кусок списка, который попал на текущую страницу.</summary>
public readonly record struct ListPage(int PageIndex, int FirstIndex, int Count)
{
    public int LastIndex => FirstIndex + Count - 1;
}

public static class TextListMessageUtils
{
    public const int ItemsPerPage = 15;

    /// <summary>
    /// Строк таблицы на странице. Таблицы крупнее простых строк (15), поэтому 10:
    /// остаток за пределами страницы показывает строка «ещё N …» над ней и под ней
    /// (<see cref="AddRemainingRow"/>), листают кнопки клавиатуры.
    /// </summary>
    public const int TableRowsPerPage = 10;

    /// <summary>Многоточие в строке-остатке: «ещё 3 друга…».</summary>
    private const string Ellipsis = "…";

    /// <summary>
    /// Строка «ещё N друзей…» — остаток списка, не влезший на страницу (сверху — то,
    /// что осталось до текущей страницы, снизу — после). Обычный текст, а не кнопка:
    /// нажатие на такую кнопку приходило битыми callback-данными, а листание и так
    /// есть кнопками клавиатуры. Подпись собирает экран — склонения из
    /// <see cref="PluralUtils"/>. Лишние ячейки справа добивает <see cref="TableBuilder"/>.
    /// </summary>
    public static void AddRemainingRow(TableBuilder table, string caption, int cellsCount = 1)
    {
        table.Row(cells =>
        {
            cells.Add(cell => cell.Verbatim(caption).Verbatim(Ellipsis));

            for (var i = 1; i < cellsCount; ++i)
                cells.Add(cell => cell.Verbatim(Ellipsis));
        });
    }

    /// <summary>Подпись строки-остатка: «ещё 3 друга».</summary>
    public static string RemainingCaption(int remaining, string itemForms) =>
        $"ещё {remaining} {itemForms}";

    public static int GetPagesCount(int totalCount, int itemsPerPage = ItemsPerPage) =>
        (int)Math.Ceiling((double)totalCount / itemsPerPage);

    /// <summary>
    /// Страница, на которой лежит элемент с указанным индексом. Индекс -1 значит «не нашли»
    /// (такое бывает у ссылок из уведомлений) — ведём на первую страницу, а не в минус.
    /// </summary>
    public static int GetPageIndex(int itemIndex, int itemsPerPage = ItemsPerPage) =>
        Math.Max(itemIndex, 0) / itemsPerPage;

    /// <summary>
    /// Кнопки листания и «Назад» для списка. Текст страницы собирает сам экран: с Rich Markdown
    /// список — это таблица или набор строк, а не «строка + перевод строки», как раньше.
    /// </summary>
    public static ListPage AddListControls<TListQuery, TParentQuery>(
        BotKeyboard keyboard,
        QueryParameterCollection parameters,
        int totalCount,
        IListPage listPosition,
        string buttonCaption = null,
        int itemsPerPage = ItemsPerPage)
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

        var pagesCount = GetPagesCount(totalCount, itemsPerPage);

        // Page can be out of range: the only item on the last page was removed,
        // or the page came from a parameter of another list (see SetListPageTo)
        pageIndex = Math.Clamp(pageIndex, 0, pagesCount - 1);

        listPosition.Page = pageIndex;

        var firstIndex = pageIndex * itemsPerPage;
        var count = Math.Min(itemsPerPage, totalCount - firstIndex);

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
