using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WishlistBot.Model;

namespace WishlistBot.Text;

public static partial class MessageTextUtils
{
    private static Regex GetDomainRegex { get; } = DomainRegex();

    /// <summary>
    /// Символы, которые Rich Markdown понимает как escape (проверено на живом Telegram:
    /// <c>\!</c>, <c>\#</c>, <c>\$</c>, <c>\(</c>, <c>\)</c>, <c>\*</c>, <c>\+</c>,
    /// <c>\-</c>, <c>\.</c>, <c>\=</c>, <c>\[</c>, <c>\\</c>, <c>\]</c>, <c>\^</c>,
    /// <c>\_</c>, <c>\`</c>, <c>\{</c>, <c>\|</c>, <c>\}</c>, <c>\~</c>, <c>\&gt;</c>).
    /// Остальная пунктуация (<c>: ; , % ? @ / " '</c>) escape не поддаётся — перед ней
    /// обратный слэш останется в тексте, поэтому её не трогаем.
    /// </summary>
    private const string EscapableCharacters = "!#$()*+-.=[\\]^_`{|}~>";

    public static string GetDomainFromLink(string link)
    {
        return GetDomainRegex.Replace(link, string.Empty);
    }

    /// <summary>
    /// Экранирует пользовательский текст под Rich Markdown. В отличие от прежнего
    /// MarkdownV2-варианта, здесь экранируется только то, что Markdown вообще понимает:
    /// иначе текст раздувается втрое и в нём появляются лишние обратные слэши.
    /// </summary>
    public static StringBuilder EscapeString(string str, StringBuilder stringBuilder = null)
    {
        var sb = stringBuilder ?? new StringBuilder();

        foreach (var ch in str)
        {
            switch (ch)
            {
                // Escape для них не работает — только HTML-сущности
                case '<':
                    sb.Append("&lt;");
                    continue;
                case '&':
                    sb.Append("&amp;");
                    continue;
            }

            if (EscapableCharacters.Contains(ch))
                sb.Append('\\');

            sb.Append(ch);
        }

        return sb;
    }

    public static string EscapeString(string str) => EscapeString(str, null).ToString();

    /// <summary>
    /// Текст внутри HTML-блока rich-сообщения (<c>&lt;blockquote expandable&gt;</c>).
    /// Markdown там не разбирается вообще: <c>_курсив_</c>, <c>**жирный**</c> и
    /// <c>`код`</c> остаются символами, а обратный слэш — обратным слэшем. Поэтому
    /// markdown-экранирование здесь вредно (именно из-за него в подтверждении рассылки
    /// было видно «Всем привет\!» и «\(взамен\)»), а нужно только HTML-экранирование:
    /// <c>&lt;</c> и <c>&amp;</c> уходят сущностями, остальное — как есть.
    /// Проверено живой отправкой: <c>rich-probe/html_block_markdown_probe.py</c>,
    /// <c>rich-probe/quote_html_escape_probe.py</c>.
    /// </summary>
    public static StringBuilder EscapeHtml(string str, StringBuilder stringBuilder = null)
    {
        var sb = stringBuilder ?? new StringBuilder();

        foreach (var ch in str)
        {
            switch (ch)
            {
                case '<':
                    sb.Append("&lt;");
                    continue;
                case '&':
                    sb.Append("&amp;");
                    continue;
            }

            sb.Append(ch);
        }

        return sb;
    }

    /// <summary>
    /// Ссылка внутри <c>[...](...)</c>: экранировать надо только то, что может закрыть
    /// саму ссылку.
    /// </summary>
    public static string EscapeLink(string link) =>
        link.Replace("\\", "\\\\").Replace(")", "\\)").Replace(" ", "%20");

    /// <summary>
    /// Идентификатор картинки внутри rich-сообщения: <c>tg://photo?id=...</c> принимает
    /// только <c>A-Z a-z 0-9 _ -</c> до 64 символов, а ключ хранилища — telegram file_id,
    /// который в эти рамки не влезает. Поэтому id — короткий хэш ключа.
    /// </summary>
    public static string GetMediaId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return "m" + Convert.ToHexString(hash, 0, 15).ToLowerInvariant();
    }

    public static string GetMediaLink(string key) => $"tg://photo?id={GetMediaId(key)}";

    public static string PriceToString(Price priceRange)
    {
        return priceRange switch
        {
            Price.NotSet => "не установлена",
            Price.Under500 => "до 500₽",
            Price.Under1000 => "от 500 до 1000₽",
            Price.Under3000 => "от 1000 до 3000₽",
            Price.Under5000 => "от 3000 до 5000₽",
            Price.Under10000 => "от 5000 до 10 000₽",
            Price.Over10000 => "больше 10 000₽",
            _ => "incorrect"
        };
    }

    public static string PriceToShortString(Price priceRange)
    {
        return priceRange switch
        {
            // Длинное тире, а не пустая ячейка: без него строка таблицы выглядит повисшей
            Price.NotSet => "\u2014",
            Price.Under500 => "<500₽",
            Price.Under1000 => "500-1к ₽",
            Price.Under3000 => "1-3к ₽",
            Price.Under5000 => "3-5к ₽",
            Price.Under10000 => "5-10к ₽",
            Price.Over10000 => ">10к ₽",
            _ => "incorrect"
        };
    }

    [GeneratedRegex(@".+\/\/|www.|\..+")]
    private static partial Regex DomainRegex();
}
