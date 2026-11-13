using Serilog;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Model;

namespace WishlistBot.Media;

/// <summary>
/// Разовая починка легаси-картинок: старый бот записывал в <c>FileId</c> file_id самого
/// маленького размера фото, поэтому импортированные в базу блобы — превью (90–320 px).
/// В сообщении такой file_id разворачивается в полную фотографию, поэтому раньше это
/// не было видно, но при перезаливке блоба картинка становилась крошечной.
///
/// Проход идёт по «подозрительно маленьким» блобам, узнаёт полный размер через
/// канал-хранилище (<see cref="MediaStorage.ResolveLargestSizeAsync"/>) и перезаписывает
/// блоб сжатым JPEG покрупнее. Результат помечается <see cref="MediaItemModel.SizeChecked"/>,
/// так что каждую картинку смотрим один раз.
/// </summary>
public class MediaUpgradeService
{
    /// <summary>Блоб мельче этого по большей стороне считаем возможным превью.</summary>
    public const int PreviewSide = 320;

    public const int BatchSize = 10;

    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(3);

    private bool _started;
    private ILogger _logger;

    public static MediaUpgradeService Instance { get; } = new();

    public void Start(ILogger logger)
    {
        if (_started)
            return;

        _started = true;
        _logger = logger;

        _ = Task.Run(() => RunLoop(logger));
    }

    private async Task RunLoop(ILogger logger)
    {
        try
        {
            if (!await HasPendingAsync())
            {
                logger.Information("No picture previews left: MediaUpgradeService can be disabled");
                return;
            }

            while (await HasPendingAsync())
            {
                var upgraded = await UpgradeBatchAsync(BatchSize);

                // Канал-хранилище недоступен: дальше долбить бессмысленно, превью
                // дочистятся по мере показа экранов (см. MediaStorage.RequestUpgradeIfPreview)
                if (upgraded is null)
                {
                    logger.Warning("Full picture sizes are unknown (storage channel is not configured or " +
                                   "Telegram refused the probe), previews stay until the pictures are shown");
                    return;
                }

                if (upgraded == 0)
                    return;

                await Task.Delay(BatchDelay);
            }

            logger.Information("Picture previews are upgraded");
        }
        catch (Exception e)
        {
            logger.Error("Picture upgrade failed: {error}", e.ToString());
        }
    }

    /// <summary>Остались ли блобы-превью, которые ещё не проверяли.</summary>
    public Task<bool> HasPendingAsync()
    {
        using var storageContext = MediaStorageContext.Create();

        return Task.FromResult(storageContext.StoredMedia.Any(IsPreviewCandidate()));
    }

    /// <summary>
    /// Один проход: проверяет до <paramref name="batchSize"/> картинок.
    /// null — полный размер не удалось узнать (канал-хранилище недоступен).
    /// </summary>
    public async Task<int?> UpgradeBatchAsync(int batchSize)
    {
        List<MediaItemModel> items;

        using (var storageContext = MediaStorageContext.Create())
        {
            items = storageContext.StoredMedia
                .Where(IsPreviewCandidate())
                .OrderBy(m => m.MediaItemId)
                .Take(batchSize)
                .ToList();
        }

        var upgraded = 0;

        foreach (var item in items)
        {
            var fileId = item.TelegramFileId ?? item.Key;
            var fullSize = await MediaStorage.Instance.ResolveLargestSizeAsync(fileId);

            if (fullSize is null)
                return null;

            if ((long)fullSize.Value.Width * fullSize.Value.Height > (long)item.Width * item.Height)
            {
                if (await MediaStorage.Instance.UpgradeAsync(item.Key, fullSize.Value.FileId, fullSize.Value.Width, fullSize.Value.Height))
                    ++upgraded;
            }
            else
            {
                // Картинка изначально такая и есть — больше не смотрим
                MarkSizeChecked(item.MediaItemId);
            }
        }

        return upgraded;
    }

    private static System.Linq.Expressions.Expression<Func<MediaItemModel, bool>> IsPreviewCandidate() =>
        item => item.Data != null
                && !item.SizeChecked
                && item.Width <= PreviewSide
                && item.Height <= PreviewSide;

    private void MarkSizeChecked(int mediaItemId)
    {
        using var storageContext = MediaStorageContext.Create();
        var stored = storageContext.StoredMedia.FirstOrDefault(m => m.MediaItemId == mediaItemId);

        if (stored is null)
            return;

        stored.SizeChecked = true;
        storageContext.SaveChanges();

        _logger?.Debug("Media [{mediaItemId}] is not a preview, nothing to upgrade", mediaItemId);
    }
}
