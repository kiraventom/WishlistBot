using System.Text;

namespace WishlistBot.Text;

public enum TableAlign
{
    Left,
    Center,
    Right
}

/// <summary>
/// Таблица Rich Markdown. Заголовок обязателен: без него Markdown таблицу не разберёт,
/// поэтому если экрану заголовок не нужен, он передаёт пустые строки.
///
/// В ячейках допустимо только инлайн-форматирование (ссылки, жирный, спойлер) — блоки
/// и переводы строк внутри ячейки Telegram не примет.
/// </summary>
public sealed class TableBuilder
{
    private readonly List<string> _headers = [];
    private readonly List<TableAlign> _aligns = [];
    private readonly List<List<string>> _rows = [];

    public IReadOnlyList<List<string>> Rows => _rows;

    /// <summary>Заголовок таблицы с форматированием в ячейках (кнопки, ссылки).</summary>
    public TableBuilder Header(params Action<MessageText>[] cells)
    {
        _headers.Clear();

        foreach (var cell in cells)
        {
            var text = new MessageText();
            cell(text);
            _headers.Add(text.ToString());
        }

        return this;
    }

    /// <summary>Заголовок таблицы: обычный текст, экранируется автоматически.</summary>
    public TableBuilder Header(params string[] headers)
    {
        _headers.Clear();
        _headers.AddRange(headers);
        return this;
    }

    /// <summary>Выравнивание колонок по порядку. По умолчанию — по левому краю.</summary>
    public TableBuilder Align(params TableAlign[] aligns)
    {
        _aligns.Clear();
        _aligns.AddRange(aligns);
        return this;
    }

    /// <summary>Строка из простого текста.</summary>
    public TableBuilder Row(params string[] cells)
    {
        var row = new List<string>(cells.Length);

        foreach (var cell in cells)
            row.Add(new MessageText(cell).ToString());

        _rows.Add(row);
        return this;
    }

    /// <summary>
    /// Строка, число ячеек которой зависит от данных (например, колонки «Цена»/«Ссылка»
    /// рисуются только когда есть что показать).
    /// </summary>
    public TableBuilder Row(Action<List<Action<MessageText>>> build)
    {
        var cells = new List<Action<MessageText>>();
        build(cells);

        return Row(cells.ToArray());
    }

    /// <summary>Строка «поле → значение»: подпись простым текстом, значение с форматированием.</summary>
    public TableBuilder Row(string label, Action<MessageText> value)
    {
        var text = new MessageText();
        value(text);

        _rows.Add([new MessageText(label).ToString(), text.ToString()]);
        return this;
    }

    /// <summary>Строка с форматированием в ячейках: ссылки, жирный, спойлер и т.п.</summary>
    public TableBuilder Row(params Action<MessageText>[] cells)
    {
        var row = new List<string>(cells.Length);

        foreach (var cell in cells)
        {
            var text = new MessageText();
            cell(text);
            row.Add(text.ToString());
        }

        _rows.Add(row);
        return this;
    }

    internal void AppendTo(StringBuilder sb)
    {
        var columnsCount = Math.Max(_headers.Count, _rows.Max(row => row.Count));
        columnsCount = Math.Max(columnsCount, _aligns.Count);

        AppendRow(sb, _headers, columnsCount);

        sb.Append('|');
        for (var i = 0; i < columnsCount; ++i)
        {
            var align = i < _aligns.Count ? _aligns[i] : TableAlign.Left;
            sb.Append(align switch
            {
                TableAlign.Right => "--:|",
                TableAlign.Center => ":-:|",
                _ => ":--|"
            });
        }

        sb.Append('\n');

        foreach (var row in _rows)
            AppendRow(sb, row, columnsCount);
    }

    private static void AppendRow(StringBuilder sb, IReadOnlyList<string> cells, int columnsCount)
    {
        sb.Append('|');

        for (var i = 0; i < columnsCount; ++i)
        {
            sb.Append(' ');
            sb.Append(i < cells.Count ? cells[i] : string.Empty);
            sb.Append(" |");
        }

        sb.Append('\n');
    }
}
