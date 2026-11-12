using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model;

namespace WishlistBot.Media;

/// <summary>
/// Фоновая догрузка картинок в базу: скачивает из Telegram всё, что ещё не импортировано
/// (старые виши и только что присланные фото), сжимает и убирает сообщения из старого
/// канала-хранилища. Когда импортировать нечего — засыпает и изредка проверяет снова.
/// </summary>
public class MediaImportService
{
    public const int BatchSize = 10;

    /// <summary>Сколько раз пробуем скачать картинку, прежде чем считать file_id мёртвым.</summary>
    public const int MaxImportAttempts = 5;

    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMinutes(5);

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
        var seeded = false;

        while (true)
        {
            try
            {
                if (!seeded)
                {
                    await MediaStorage.Instance.SeedFromUserDatabaseAsync();
                    seeded = true;
                }

                var imported = await ImportBatchAsync(BatchSize);
                await Task.Delay(imported > 0 ? BatchDelay : IdleDelay);
            }
            catch (Exception e)
            {
                logger.Error("Media import failed: {error}", e.ToString());
                await Task.Delay(IdleDelay);
            }
        }
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
