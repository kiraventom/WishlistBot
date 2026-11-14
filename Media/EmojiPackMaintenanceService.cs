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

    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);
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
        while (true)
        {
            try
            {
                while (await EmojiPackService.Instance.BackfillAsync(BatchSize) > 0)
                    await Task.Delay(BatchDelay);

                await EmojiPackService.Instance.CheckPacksAsync();
            }
            catch (Exception e)
            {
                logger.Error("Emoji pack maintenance failed: {error}", e.ToString());
            }

            await Task.Delay(CheckInterval);
        }
    }
}
