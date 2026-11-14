namespace WishlistBot.Model.User;

/// <summary>
/// Жёсткие пределы вишлиста.
///
/// <see cref="MaxWishes"/> существует не из вредности: каждая картинка виша — это ещё
/// и стикер в наборе кастомных эмодзи пользователя, а в наборе Telegram разрешает 200
/// стикеров. Кап в 100 оставляет половину набора в запасе — на стикеры, которые не успели
/// удалить за удалёнными вишами.
/// </summary>
public static class WishLimits
{
    public const int MaxWishes = 100;

    public static bool IsFull(int wishesCount) => wishesCount >= MaxWishes;
}
