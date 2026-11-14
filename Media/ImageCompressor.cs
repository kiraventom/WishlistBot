using SkiaSharp;

namespace WishlistBot.Media;

/// <summary>Сжатая картинка: JPEG и её фактические размеры.</summary>
public sealed record CompressedImage(byte[] Data, int Width, int Height);

/// <summary>
/// Приводит присланное пользователем фото к виду, который не жалко держать в базе.
///
/// Большая сторона ужимается до <see cref="MaxSide"/> (как раз то, до чего Telegram сам
/// сжимает фото при отправке, поэтому качество не теряется), младше — не растягиваем.
/// Дальше JPEG с качеством из <see cref="Qualities"/> по убыванию, пока не влезем
/// в <see cref="MaxBytes"/>. Всё, что не удалось декодировать, считается «не картинкой».
/// </summary>
public static class ImageCompressor
{
    public const int MaxSide = 1280;
    public const long MaxBytes = 1024 * 1024;

    /// <summary>
    /// Сторона миниатюры-эмодзи. Telegram требует ровно 100×100 для статичных
    /// кастомных эмодзи и отвечает <c>STICKER_PNG_DIMENSIONS</c> на любой другой размер
    /// (проверено живой отправкой: <c>rich-probe/custom_emoji_set_probe.py</c>).
    /// </summary>
    public const int EmojiSize = 100;

    private const int EmojiQuality = 90;

    private static readonly int[] Qualities = [85, 75, 65];

    /// <summary>
    /// Миниатюра виша для ячейки таблицы: центральный квадрат картинки, сжатый до
    /// <see cref="EmojiSize"/>×<see cref="EmojiSize"/> в WEBP. Квадрат берём из центра,
    /// а не растягиваем: фото 1000×800 отдаёт всё, что левее и правее x=100, и не теряет
    /// пропорции.
    /// </summary>
    /// <returns>null, если это не картинка или она мельче 100 пикселей по меньшей стороне.</returns>
    public static byte[] CompressEmoji(byte[] source)
    {
        if (source is null || source.Length == 0)
            return null;

        try
        {
            using var data = SKData.CreateCopy(source);
            using var decoded = SKImage.FromEncodedData(data);

            if (decoded is null)
                return null;

            using var bitmap = SKBitmap.FromImage(decoded);

            var side = Math.Min(bitmap.Width, bitmap.Height);

            // Мелкие картинки не растягиваем: из превью 90×67 ничего, кроме мыла, не выйдет
            if (side < EmojiSize)
                return null;

            var left = (bitmap.Width - side) / 2;
            var top = (bitmap.Height - side) / 2;

            using var square = new SKBitmap(new SKImageInfo(EmojiSize, EmojiSize, SKColorType.Rgba8888, SKAlphaType.Premul));

            using (var canvas = new SKCanvas(square))
            {
                canvas.DrawBitmap(
                    bitmap,
                    new SKRect(left, top, left + side, top + side),
                    new SKRect(0, 0, EmojiSize, EmojiSize),
                    new SKSamplingOptions(SKCubicResampler.Mitchell),
                    null);
            }

            using var image = SKImage.FromBitmap(square);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, EmojiQuality);

            return encoded?.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <returns>null, если данные не являются картинкой, которую можно перекодировать.</returns>
    public static CompressedImage Compress(byte[] source)
    {
        if (source is null || source.Length == 0)
            return null;

        try
        {
            using var data = SKData.CreateCopy(source);
            using var decoded = SKImage.FromEncodedData(data);

            if (decoded is null)
                return null;

            using var bitmap = SKBitmap.FromImage(decoded);

            var scale = Math.Min(1f, (float)MaxSide / Math.Max(bitmap.Width, bitmap.Height));
            var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

            using var resized = scale < 1f
                ? bitmap.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell))
                : null;

            using var scaled = SKImage.FromBitmap(resized ?? bitmap);

            byte[] smallest = null;

            foreach (var quality in Qualities)
            {
                using var encoded = scaled.Encode(SKEncodedImageFormat.Jpeg, quality);
                if (encoded is null)
                    return null;

                var bytes = encoded.ToArray();

                if (smallest is null || bytes.Length < smallest.Length)
                    smallest = bytes;

                if (bytes.Length <= MaxBytes)
                    return new CompressedImage(bytes, width, height);
            }

            // Даже худшее качество не влезло в лимит — отдаём самое маленькое, что получилось
            return new CompressedImage(smallest, width, height);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
