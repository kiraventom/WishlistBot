namespace WishlistBot.Text;

/// <summary>
/// Даты по-русски без участия <see cref="System.Globalization.CultureInfo"/>: культура
/// зависит от ICU в окружении (на урезанном образе месяц окажется английским или
/// инвариантным), а формат в боте должен быть один и тот же. Месяцы — в родительном
/// падеже, как в «10 октября».
/// </summary>
public static class RussianDate
{
    private static readonly string[] MonthsGenitive =
    [
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря"
    ];

    /// <summary>«10 октября» — день без ведущего нуля и длинное название месяца, без года.</summary>
    public static string DayMonth(DateOnly date) => $"{date.Day} {MonthsGenitive[date.Month - 1]}";
}
