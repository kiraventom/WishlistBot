namespace WishlistBot.Text;

/// <summary>
/// Русские формы слов по числу: 1 виш, 2 виша, 5 вишей.
/// </summary>
public static class PluralUtils
{
    public static string Ru(int count, string one, string few, string many)
    {
        var lastTwoDigits = count % 100;
        if (lastTwoDigits is >= 11 and <= 14)
            return many;

        return (count % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many,
        };
    }

    public static string Days(int count) => Ru(count, "день", "дня", "дней");

    public static string Years(int count) => Ru(count, "год", "года", "лет");

    public static string Wishes(int count) => Ru(count, "виш", "виша", "вишей");
}
