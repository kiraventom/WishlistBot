using System.Text;
using WishlistBot.Model.User;

namespace WishlistBot.Text;

/// <summary>
/// Сборка текста сообщения в «богатом» Markdown (Rich Markdown, Bot API 10.x): заголовки,
/// списки, таблицы, цитаты, сворачиваемые блоки, медиа-блоки.
///
/// Отличия от прежнего MarkdownV2-билдера, о которых легко забыть:
/// <list type="bullet">
/// <item>экранируются не все ASCII-символы, а только те, что Telegram понимает как
/// escape (см. <see cref="MessageTextUtils.EscapeString"/>); <c>&lt;</c> и <c>&amp;</c>
/// escaping не поддаются — они уходят HTML-сущностями;</item>
/// <item>одиночный перевод строки в Markdown — это пробел, поэтому
/// <see cref="LineBreak"/> ставит «жёсткий» перенос (два пробела перед \n);</item>
/// <item>блочные элементы (заголовок, таблица, список, цитата) отделяются от соседей
/// пустой строкой — иначе Markdown склеит их с абзацем.</item>
/// </list>
/// </summary>
public sealed partial class MessageText
{
    /// <summary>Жёсткий перенос строки внутри абзаца: два пробела перед переводом строки.</summary>
    private const string HardBreak = "  \n";

    private readonly StringBuilder _sb = new();
    private readonly List<string> _mediaKeys = [];

    public MessageText()
    {
    }

    public MessageText(string text) => Verbatim(text);

    /// <summary>Ключи картинок, на которые ссылается текст (в порядке появления).</summary>
    public IReadOnlyList<string> MediaKeys => _mediaKeys;

    public bool IsEmpty => _sb.Length == 0;

    /// <summary>Перевод строки внутри абзаца.</summary>
    public MessageText LineBreak()
    {
        _sb.Append(HardBreak);
        return this;
    }

    /// <summary>Пустая строка между блоками. Повторные вызовы не копят пустые строки.</summary>
    public MessageText BlockBreak()
    {
        TrimTrailingSpaces();

        if (_sb.Length == 0)
            return this;

        if (EndsWithBlankLine())
            return this;

        _sb.Append('\n').Append('\n');
        return this;
    }

    public MessageText Verbatim(string text)
    {
        if (text is null)
            return this;

        MessageTextUtils.EscapeString(text, _sb);

        return this;
    }

    public MessageText Bold(string text) => Wrap("**", text);

    public MessageText Italic(string text) => Wrap("_", text);

    public MessageText ItalicBold(string text) => Wrap("**_", "_**", text);

    public MessageText Strikethrough(string text) => Wrap("~~", text);

    /// <summary>Выделение маркером. В MarkdownV2 такого не было.</summary>
    public MessageText Marked(string text) => Wrap("==", text);

    /// <summary>Подчёркивание: в Rich Markdown только HTML-тегом.</summary>
    public MessageText Underline(string text)
    {
        _sb.Append("<u>");
        Verbatim(text);
        _sb.Append("</u>");

        return this;
    }

    /// <summary>
    /// Спойлер. В Markdown это <c>||текст||</c>, но внутри ячейки таблицы вертикальная черта
    /// экранируется (иначе она разрежет строку), и спойлер ломается — поэтому HTML-тег,
    /// который работает и в тексте, и в ячейке.
    /// </summary>
    public MessageText Spoiler(string text) => Wrap("<tg-spoiler>", "</tg-spoiler>", text);

    /// <summary>
    /// Моноширинный фрагмент. Однострочный — инлайн-код, многострочный — блок кода.
    /// </summary>
    public MessageText Monospace(string text)
    {
        if (text is null)
            return this;

        if (text.Contains('\n'))
        {
            EnsureBlockStart();
            _sb.Append("```\n").Append(text).Append("\n```");
            return BlockBreak();
        }

        // Внутри кода Markdown не разбирается, поэтому текст не экранируем — только
        // подбираем длину ограничителя, чтобы обратные кавычки в тексте его не закрыли
        var fence = new string('`', LongestBacktickRun(text) + 1);
        _sb.Append(fence);

        if (text.StartsWith('`') || text.EndsWith('`'))
            _sb.Append(' ').Append(text).Append(' ');
        else
            _sb.Append(text);

        _sb.Append(fence);

        return this;
    }

    public MessageText InlineUrl(string text, string link)
    {
        _sb.Append('[');
        Verbatim(text);
        _sb.Append(']');

        _sb.Append('(').Append(MessageTextUtils.EscapeLink(link)).Append(')');
        return this;
    }

    /// <summary>
    /// Ссылка, текст которой собирается форматированием (например, зачёркнутое название
    /// забронированного виша): <c>InlineUrl(text, url)</c> экранировал бы маркеры.
    /// </summary>
    public MessageText InlineUrl(Action<MessageText> text, string link)
    {
        _sb.Append('[');
        text(this);
        _sb.Append(']');

        _sb.Append('(').Append(MessageTextUtils.EscapeLink(link)).Append(')');
        return this;
    }

    public MessageText InlineUrl(string link)
    {
        var domainName = MessageTextUtils.GetDomainFromLink(link);
        domainName = char.ToUpper(domainName[0]) + domainName[1..];
        return InlineUrl(domainName, link);
    }

    public MessageText InlineMention(UserModel userModel, string text = null)
    {
        text ??= userModel.FirstName;

        return string.IsNullOrEmpty(userModel.Tag)
            ? InlineMention(text, userModel.TelegramId)
            : InlineMention(text, userModel.Tag);
    }

    public MessageText InlineMention(string text, string tag)
    {
        _sb.Append('[');
        Verbatim(text);
        _sb.Append(']');
        _sb.Append("(t.me/").Append(tag).Append(')');

        return this;
    }

    public MessageText InlineMention(string text, long userId)
    {
        _sb.Append('[');
        Verbatim(text);
        _sb.Append(']');
        _sb.Append("(tg://user?id=").Append(userId).Append(')');

        return this;
    }

    /// <summary>Заголовок экрана (## по умолчанию — крупный, но не гигантский).</summary>
    public MessageText Heading(string text, int level = 2)
    {
        EnsureBlockStart();

        for (var i = 0; i < Math.Clamp(level, 1, 6); ++i)
            _sb.Append('#');

        _sb.Append(' ');
        Verbatim(text);

        return BlockBreak();
    }

    public MessageText Heading(Action<MessageText> content, int level = 2)
    {
        EnsureBlockStart();

        for (var i = 0; i < Math.Clamp(level, 1, 6); ++i)
            _sb.Append('#');

        _sb.Append(' ');
        content(this);

        return BlockBreak();
    }

    public MessageText Divider()
    {
        EnsureBlockStart();
        _sb.Append("---");
        return BlockBreak();
    }

    /// <summary>Обычная цитата. Внутри можно форматировать текст, но не вставлять блоки.</summary>
    public MessageText Quote(string text)
    {
        if (text is null)
            return this;

        EnsureBlockStart();

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; ++i)
        {
            _sb.Append("> ");
            Verbatim(lines[i]);

            if (i < lines.Length - 1)
                _sb.Append('\n');
        }

        return BlockBreak();
    }

    /// <summary>Сворачиваемая цитата: в Markdown синтаксиса нет, только HTML.</summary>
    public MessageText ExpandableQuote(string text)
    {
        if (text is null)
            return this;

        EnsureBlockStart();
        _sb.Append("<blockquote expandable>");

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; ++i)
        {
            if (i > 0)
                _sb.Append("<br>");

            Verbatim(lines[i]);
        }

        _sb.Append("</blockquote>");
        return BlockBreak();
    }

    /// <summary>Маркированный список.</summary>
    public MessageText Bullets(int count, Action<MessageText, int> item)
    {
        if (count <= 0)
            return this;

        EnsureBlockStart();

        for (var i = 0; i < count; ++i)
        {
            _sb.Append("- ");
            item(this, i);
            _sb.Append('\n');
        }

        return BlockBreak();
    }

    public MessageText Bullets(params string[] items) =>
        Bullets(items.Length, (text, index) => text.Verbatim(items[index]));

    /// <summary>
    /// Список с чекбоксами: состояние видно прямо в тексте (подарено / в планах,
    /// включено / выключено).
    /// </summary>
    public MessageText TaskList(int count, Func<int, bool> isChecked, Action<MessageText, int> item)
    {
        if (count <= 0)
            return this;

        EnsureBlockStart();

        for (var i = 0; i < count; ++i)
        {
            _sb.Append(isChecked(i) ? "- [x] " : "- [ ] ");
            item(this, i);
            _sb.Append('\n');
        }

        return BlockBreak();
    }

    /// <summary>Таблица. Первая строка — заголовок (Markdown без него не умеет).</summary>
    public MessageText Table(Action<TableBuilder> build)
    {
        var builder = new TableBuilder();
        build(builder);

        if (builder.Rows.Count == 0)
            return this;

        EnsureBlockStart();
        builder.AppendTo(_sb);

        return BlockBreak();
    }

    /// <summary>Сворачиваемый блок: Markdown внутри разбирается, блоки внутри — тоже.</summary>
    public MessageText Details(string summary, Action<MessageText> body, bool isOpen = false)
    {
        EnsureBlockStart();

        _sb.Append(isOpen ? "<details open><summary>" : "<details><summary>");
        Verbatim(summary);
        _sb.Append("</summary>\n\n");

        body(this);

        TrimTrailingSpaces();
        _sb.Append("\n\n</details>");

        return BlockBreak();
    }

    /// <summary>
    /// Картинка блоком в теле сообщения. Ключ — тот же, что у <c>WishModel.MediaKey</c>
    /// и <c>BroadcastModel.MediaKey</c>: по нему отправитель достаёт блоб из хранилища.
    /// </summary>
    public MessageText Media(string key, string caption = null)
    {
        if (key is null)
            return this;

        EnsureBlockStart();
        _sb.Append("![](").Append(MessageTextUtils.GetMediaLink(key));

        if (!string.IsNullOrEmpty(caption))
            _sb.Append(' ').Append('"').Append(caption.Replace('"', '\'')).Append('"');

        _sb.Append(')');
        _mediaKeys.Add(key);

        return BlockBreak();
    }

    public override string ToString()
    {
        TrimTrailingSpaces();

        // Больше одной пустой строки подряд Markdown всё равно не покажет, а в отладке мешает
        var text = ExcessiveNewLinesRegex().Replace(_sb.ToString(), "\n\n");

        return text.TrimEnd('\n');
    }

    private bool EndsWithBlankLine() =>
        _sb.Length >= 2 && _sb[_sb.Length - 1] == '\n' && _sb[_sb.Length - 2] == '\n';

    [System.Text.RegularExpressions.GeneratedRegex(@"\n{3,}")]
    private static partial System.Text.RegularExpressions.Regex ExcessiveNewLinesRegex();

    private MessageText Wrap(string marker, string text) => Wrap(marker, marker, text);

    /// <summary>
    /// Пробелы выносятся за маркеры: Markdown не считает форматирование закрытым,
    /// если перед закрывающим маркером стоит пробел (<c>**Название: **</c> ничего не выделит).
    /// </summary>
    private MessageText Wrap(string openMarker, string closeMarker, string text)
    {
        if (text is null)
            return this;

        var first = 0;
        while (first < text.Length && char.IsWhiteSpace(text[first]))
            ++first;

        var last = text.Length;
        while (last > first && char.IsWhiteSpace(text[last - 1]))
            --last;

        Verbatim(text[..first]);

        _sb.Append(openMarker);
        Verbatim(text[first..last]);
        _sb.Append(closeMarker);

        Verbatim(text[last..]);

        return this;
    }

    /// <summary>Блочный элемент не должен прилипать к предыдущему абзацу.</summary>
    private void EnsureBlockStart()
    {
        TrimTrailingSpaces();

        if (_sb.Length > 0 && !EndsWithBlankLine())
            _sb.Append("\n\n");
    }

    private void TrimTrailingSpaces()
    {
        var length = _sb.Length;

        while (length > 0 && (_sb[length - 1] == ' ' || _sb[length - 1] == '\t'))
            --length;

        if (length != _sb.Length)
            _sb.Length = length;
    }

    private static int LongestBacktickRun(string text)
    {
        var longest = 0;
        var current = 0;

        foreach (var ch in text)
        {
            current = ch == '`' ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }
}
