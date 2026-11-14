using Serilog;

namespace WishlistBot.Media;

/// <summary>
/// Фоновая часть миниатюр вишей: догружает недостающие (старые виши, картинки, добравшиеся
/// из фона, и всё, что не собралось с первого раза) и раз в сутки сверяет наборы с Telegram.
///
/// Новые фото собираются сразу после апдейта (<see cref="MediaStorage.FlushAsync"/>), фон им
/// не нужен — здесь только хвосты. Пачками и с паузой: у Telegram есть лимиты на создание
/// стикеров, а 2 CPU контейнера не любят спешки.
/// </summary>
public class EmojiPackMaintenanceService
{
    public const int BatchSize = 5;

    /// <summary>Предохранитель от бесконечного цикла, если Telegram начнёт отказывать на всём.</summary>
    public const int MaxBatches = 200;

    /// <summary>Сколько пользователей за один проход сверяем по аватаркам (1 запрос на человека).</summary>
    public const int AvatarBatchSize = 50;

    /// <summary>
    /// Сколько раз быстро повторить проход после старта. Нужно из-за гонки с фоновой
    /// догрузкой блобов: на старте часть картинок ещё без блобов, они не попадают в очередь.
    /// </summary>
    public const int QuickPasses = 6;

    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan QuickPassDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private bool _started;

    public static EmojiPackMaintenanceService Instance { get; } = new();

    public void Start(ILogger logger)
    {
        if (_started)
            return;

        _started = true;
        _ = Task.Run(() => RunLoop(logger));
    }

    private static async Task RunLoop(ILogger logger)
    {
        // Первый проход — сразу, следующие — с паузой: к этому моменту фоновая догрузка
        // успевает импортировать старые блобы, и их миниатюры уже есть из чего собрать
        for (var pass = 0; pass < QuickPasses; ++pass)
        {
            await RunPassAsync(logger, checkPacks: pass == 0, retryFailed: false);

            if (pass < QuickPasses - 1)
                await Task.Delay(QuickPassDelay);
        }

        while (true)
        {
            await Task.Delay(CheckInterval);
            await RunPassAsync(logger, checkPacks: true, retryFailed: true);
        }
    }

    private static async Task RunPassAsync(ILogger logger, bool checkPacks, bool retryFailed)
    {
        try
        {
            // Сначала снимаем с очереди всё, что миниатюр не заслуживает (рассылки, хвосты
            // удалённых вишей): иначе догрузка стоит на первых же безхозных картинках
            await EmojiPackService.Instance.MarkOwnerlessMediaAsync();

            var batches = 0;

            while (batches++ < MaxBatches && await EmojiPackService.Instance.BackfillAsync(BatchSize) > 0)
                await Task.Delay(BatchDelay);

            if (checkPacks)
            {
                await EmojiPackService.Instance.CheckPacksAsync(retryFailed);

                // Аватарки меняются без уведомления боту, поэтому их опрашиваем:
                // на старте и раз в сутки, тем же проходом, что и сверку наборов.
                // Пачками, пока есть кого проверять (устаревшие — по CheckedAt — уходят в конец)
                var avatarPasses = 0;

                while (avatarPasses++ < MaxBatches
                    && await AvatarPackService.Instance.SyncAsync(AvatarBatchSize) == AvatarBatchSize)
                    await Task.Delay(BatchDelay);
            }
        }
        catch (Exception e)
        {
            logger.Error("Emoji pack maintenance failed: {error}", e.ToString());
        }
    }
}
