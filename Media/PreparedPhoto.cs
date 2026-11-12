namespace WishlistBot.Media;

/// <summary>
/// Картинка, готовая к отправке в Telegram: либо telegram file_id (кэш), либо сам JPEG.
/// Ровно одно из двух заполнено.
/// </summary>
public sealed class PreparedPhoto
{
    private PreparedPhoto(string key, string fileId, byte[] data)
    {
        Key = key;
        FileId = fileId;
        Data = data;
    }

    /// <summary>Ключ картинки в медиа-хранилище (нужен, чтобы обновить кэш file_id).</summary>
    public string Key { get; }

    public string FileId { get; }
    public byte[] Data { get; }

    public static PreparedPhoto FromFileId(string key, string fileId) => new(key, fileId, null);

    public static PreparedPhoto FromData(string key, byte[] data) => new(key, null, data);
}
