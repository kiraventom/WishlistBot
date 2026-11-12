using Telegram.Bot.Types;

namespace WishlistBot.Media;

public static class PhotoUtils
{
    /// <summary>
    /// Самый крупный размер присланного фото. Порядок <see cref="PhotoSize"/> в апдейте
    /// Telegram не гарантирует, поэтому сравниваем по площади, а не берём первый/последний.
    /// </summary>
    public static PhotoSize Largest(this IEnumerable<PhotoSize> photos) =>
        photos?.OrderByDescending(photo => (long)photo.Width * photo.Height).FirstOrDefault();
}
