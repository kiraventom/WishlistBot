using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model;

public class MediaStorageContext : DbContext
{
    public DbSet<MediaItemModel> StoredMedia { get; set; }

    public MediaStorageContext(DbContextOptions<MediaStorageContext> options) : base(options)
    {
    }

    public static MediaStorageContext Create()
    {
        var builder = new DbContextOptionsBuilder<MediaStorageContext>();
        builder.UseSqlite(Config.Instance.MediaStorageConnectionString);

        return new MediaStorageContext(builder.Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItemModel>().HasIndex(e => e.Key).IsUnique();
        modelBuilder.Entity<MediaItemModel>().HasIndex(e => e.TelegramFileId);
    }
}

/// <summary>
/// Картинка виша (или рассылки), которая лежит блобом в базе.
///
/// Раньше медиа жило в канале-хранилище, а в базе был только telegram file_id: так file_id
/// не умирал вместе с сообщением пользователя. Теперь файл хранится у нас, а file_id —
/// только кэш: пока Telegram его принимает, картинку не перезаливаем, а если перестал
/// (файл удалён, сменился токен бота) — заливаем блоб заново и обновляем кэш.
/// </summary>
public class MediaItemModel
{
    [Key] public int MediaItemId { get; set; }

    /// <summary>
    /// Стабильный ключ, на который ссылаются виши, черновики и рассылки.
    /// У старых записей это telegram file_id, с которого началось хранение.
    /// </summary>
    [Required] public string Key { get; set; }

    /// <summary>Кэш telegram file_id. null — кэша нет, отправляем блобом.</summary>
    public string TelegramFileId { get; set; }

    /// <summary>Сжатый JPEG. null — ещё не импортирован (или файл уже недоступен в Telegram).</summary>
    public byte[] Data { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>
    /// Легаси: сообщение в канале-хранилище, из которого картинка ещё не импортирована.
    /// После успешного импорта сообщение удаляется, а поле обнуляется.
    /// </summary>
    public int? LegacyMessageId { get; set; }

    /// <summary>Сколько раз пытались скачать картинку из Telegram (чтобы не долбить мёртвый file_id).</summary>
    public int ImportAttempts { get; set; }

    /// <summary>
    /// Id миниатюры виша в наборе кастомных эмодзи владельца: именно это число идёт
    /// в разметку ячейки таблицы (<c>![](tg://emoji?id=...)</c>). null — миниатюры нет
    /// (блоба ещё нет, он мелкий, или Telegram не дал создать стикер).
    /// </summary>
    public string CustomEmojiId { get; set; }

    /// <summary>
    /// Telegram file_id стикера. Хранится отдельно, потому что <c>deleteStickerFromSet</c>
    /// принимает только file_id стикера, а не его emoji id.
    /// </summary>
    public string CustomEmojiFileId { get; set; }

    /// <summary>Чей это набор — из него удалять стикер. null, пока миниатюры нет.</summary>
    public int? EmojiOwnerUserId { get; set; }

    /// <summary>
    /// Сколько раз пробовали сделать миниатюру и не смогли: чтобы не долбить Telegram
    /// на каждой правке виша с неподходящей картинкой.
    /// </summary>
    public int ThumbAttempts { get; set; }

    /// <summary>
    /// Блоб проверен на «это превью?». Старый бот хранил file_id самого маленького размера,
    /// поэтому у легаси-картинок в базе лежит превью; <see cref="WishlistBot.Media.MediaUpgradeService"/>
    /// находит такие блобы и перекачивает их в полном размере один раз.
    /// </summary>
    public bool SizeChecked { get; set; }

    public DateTime CreatedAt { get; set; }
}
