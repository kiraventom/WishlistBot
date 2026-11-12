namespace WishlistBot.BotMessages;

/// <summary>
/// Какой запрос открывает экран: <see cref="MessageFactory"/> собирает по этим атрибутам
/// таблицу «запрос → экран», чтобы не держать ручной <c>switch</c>.
/// На один экран может приходиться несколько запросов.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MessageForAttribute(Type queryType) : Attribute
{
    public Type QueryType { get; } = queryType;
}
