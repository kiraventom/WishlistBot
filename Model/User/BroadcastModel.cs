using System.ComponentModel.DataAnnotations;

namespace WishlistBot.Model.User;

public class BroadcastModel
{
    [Key] public int BroadcastId { get; set; }

    [Required] public string Text { get; set; }
    /// <summary>Ключ картинки в медиа-хранилище (см. <see cref="MediaItemModel"/>).</summary>
    public string MediaKey { get; set; }
    public DateTime? DateTimeSent { get; set; }
    public bool Deleted { get; set; }

    public string BuildLink() => DeepLink.BuildBroadcast(BroadcastId);

    /// <summary>
    /// Превью текста рассылки — одной строкой. Текст рассылки пишется свободно и бывает
    /// многострочным, а превью идёт в ячейку таблицы, где перевод строки рвёт строку
    /// на две (см. <c>TableBuilder.ToCellText</c>).
    /// </summary>
    public string GetShortText()
    {
        if (Text is null)
            return "<empty>";

        // null вместо набора разделителей — «резать по любому пробельному символу»
        // (задокументированное поведение string.Split): переводы строк и повторные
        // пробелы превращаются в один пробел между словами.
        var text = string.Join(' ', Text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

        if (text.Length == 0)
            return "<empty>";

        return text.Length <= 50
           ? text
           : text[..49] + "…";
    }
}

