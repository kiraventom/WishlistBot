namespace WishlistBot.Model.User;

/// <summary>
/// Дата рождения: день и месяц есть всегда, год — только если человек его не скрыл.
/// Так умеет Telegram (год в профиле можно скрыть), поэтому «10 марта» без года —
/// полноценная дата рождения, а не битая. Там, где нужен возраст, смотреть на
/// <see cref="HasYear"/>: без года возраст не считаем вообще, а не выдумываем.
/// </summary>
public readonly record struct Birthday(int Day, int Month, int? Year)
{
    /// <summary>Год рождения известен — можно считать возраст.</summary>
    public bool HasYear => Year is not null;

    /// <summary>Дата рождения целиком; null, если год скрыт.</summary>
    public DateOnly? BirthDate => Year is { } year && IsValidDate(year, Month, Day) ? new DateOnly(year, Month, Day) : null;

    /// <summary>
    /// День рождения в указанном году. 29 февраля в невисокосный год считается 28-м —
    /// иначе дата просто не существует и половина календаря ломается.
    /// </summary>
    public DateOnly InYear(int year)
    {
        var day = Month == 2 && Day == 29 && !DateTime.IsLeapYear(year) ? 28 : Day;
        return new DateOnly(year, Month, day);
    }

    /// <summary>День и месяц существуют как дата (29.02 проверяем по високосному году).</summary>
    public static bool IsValidDayMonth(int day, int month) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2000, month);

    /// <summary>Год похож на год рождения: не из будущего и не из XIX века.</summary>
    public static bool IsValidYear(int year) => year >= MinYear && year <= MaxYear;

    /// <summary>Самая ранняя дата, которую считаем датой рождения (а не мусором).</summary>
    public const int MinYear = 1900;

    /// <summary>Позже текущего года день рождения быть не может.</summary>
    public static int MaxYear => DateTime.Now.Year;

    private static bool IsValidDate(int year, int month, int day) =>
        IsValidYear(year) && IsValidDayMonth(day, month) && day <= DateTime.DaysInMonth(year, month);

    /// <summary>«10.03.1990» или «10.03», если год скрыт.</summary>
    public override string ToString() =>
        HasYear ? $"{Day:00}.{Month:00}.{Year:0000}" : $"{Day:00}.{Month:00}";

    /// <summary>Полная дата — это тоже дата рождения (год просто известен).</summary>
    public static implicit operator Birthday(DateOnly date) => new(date.Day, date.Month, date.Year);
}
