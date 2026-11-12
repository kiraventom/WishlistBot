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

    private static readonly int[] Qualities = [85, 75, 65];

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
