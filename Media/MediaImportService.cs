using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model;

namespace WishlistBot.Media;

/// <summary>
/// Разовая догрузка старых картинок: скачивает из Telegram всё, что лежит в канале-хранилище
/// и ещё не стало блобом, сжимает и убирает сообщения канала. Когда импортировать нечего —
/// пишет варнинг и больше не просыпается: новые фото сжимаются сразу после апдейта
/// (<see cref="MediaStorage.FlushAsync"/>), фон им не нужен.
/// </summary>
public class MediaImportService
{
    public const int BatchSize = 10;

    /// <summary>Сколько раз пробуем скачать картинку, прежде чем считать file_id мёртвым.</summary>
    public const int MaxImportAttempts = 5;

    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    private bool _started;

    public static MediaImportService Instance { get; } = new();

    public void Start(ILogger logger)
    {
        if (_started)
            return;

        _started = true;
        _ = Task.Run(() => RunLoop(logger));
    }

    private async Task RunLoop(ILogger logger)
    {
        try
        {
            await MediaStorage.Instance.SeedFromUserDatabaseAsync();

            while (await HasPendingAsync())
            {
                var imported = await ImportBatchAsync(BatchSize);

                if (imported > 0)
                {
                    await Task.Delay(BatchDelay);
                    continue;
                }

                // Есть строки, но скачать не вышло — не долбим Telegram в цикле
                await Task.Delay(RetryDelay);
            }

            logger.Warning("No legacy media is left: MediaImportService can be disabled");
        }
        catch (Exception e)
        {
            logger.Error("Media import failed: {error}", e.ToString());
        }
    }

    /// <summary>Остались ли картинки, которые ещё не стали блобом.</summary>
    public Task<bool> HasPendingAsync()
    {
        using var storageContext = MediaStorageContext.Create();

        return Task.FromResult(storageContext.StoredMedia.Any(m => m.Data == null && m.TelegramFileId != null && m.ImportAttempts < MaxImportAttempts));
    }

    /// <summary>Один проход импорта. Возвращает число успешно импортированных картинок.</summary>
    public async Task<int> ImportBatchAsync(int maxItems)
    {
        List<MediaItemModel> pending;

        using (var storageContext = MediaStorageContext.Create())
        {
            pending = storageContext.StoredMedia
                .Where(m => m.Data == null && m.TelegramFileId != null && m.ImportAttempts < MaxImportAttempts)
                .OrderBy(m => m.MediaItemId)
                .Take(maxItems)
                .ToList();
        }

        var imported = 0;

        foreach (var mediaItem in pending)
        {
            if (!await MediaStorage.Instance.ImportAsync(mediaItem))
            {
                MediaStorage.Instance.CountImportFailure(mediaItem.MediaItemId);
                continue;
            }

            ++imported;

            // Картинка уже у нас — сообщение в старом канале больше не нужно
            if (mediaItem.LegacyMessageId is { } messageId)
                await MediaStorage.Instance.DeleteLegacyChannelMessageAsync(mediaItem.MediaItemId, messageId);
        }

        return imported;
    }
}
