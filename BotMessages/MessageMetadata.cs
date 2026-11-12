using System.Reflection;
using WishlistBot.Model.User;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

/// <summary>
/// Что экрану разрешено: какие параметры запроса он принимает
/// (<see cref="AllowedTypesAttribute"/>) и страницы каких списков ему можно менять
/// (<see cref="AllowedListPositionsAttribute"/>). Атрибуты наследуются по цепочке
/// <see cref="ChildMessageAttribute"/> от родительского экрана.
///
/// Результат разбора кэшируется по типу экрана: атрибуты не меняются в рантайме,
/// а рефлексия на каждый апдейт не нужна.
/// </summary>
internal static class MessageMetadata
{
    private static readonly Dictionary<Type, IReadOnlyCollection<QueryParameterType>> _allowedTypes = [];
    private static readonly Dictionary<Type, IReadOnlyCollection<ListPosition>> _allowedPositions = [];

    public static IReadOnlyCollection<QueryParameterType> GetAllowedTypes(Type messageType)
    {
        lock (_allowedTypes)
        {
            if (!_allowedTypes.TryGetValue(messageType, out var allowedTypes))
                _allowedTypes[messageType] = allowedTypes = CollectAllowedTypes(messageType);

            return allowedTypes;
        }
    }

    public static IReadOnlyCollection<ListPosition> GetAllowedListPositions(Type messageType)
    {
        lock (_allowedPositions)
        {
            if (!_allowedPositions.TryGetValue(messageType, out var allowedPositions))
                _allowedPositions[messageType] = allowedPositions = CollectAllowedListPositions(messageType);

            return allowedPositions;
        }
    }

    private static IReadOnlyCollection<QueryParameterType> CollectAllowedTypes(Type messageType)
    {
        var allAllowedTypes = new HashSet<QueryParameterType>(CollectParentAllowedTypes(messageType));

        foreach (var allowedTypesAttribute in messageType.GetCustomAttributes<AllowedTypesAttribute>())
            allAllowedTypes.UnionWith(allowedTypesAttribute.AllowedTypes);

        return allAllowedTypes;
    }

    private static IReadOnlyCollection<QueryParameterType> CollectParentAllowedTypes(Type messageType)
    {
        var parentType = GetParentMessageType(messageType);
        return parentType is null ? [] : GetAllowedTypes(parentType);
    }

    private static IReadOnlyCollection<ListPosition> CollectAllowedListPositions(Type messageType)
    {
        var allAllowedPositions = new HashSet<ListPosition>(CollectParentAllowedListPositions(messageType));

        foreach (var allowedPositionsAttribute in messageType.GetCustomAttributes<AllowedListPositionsAttribute>())
            allAllowedPositions.UnionWith(allowedPositionsAttribute.AllowedPositions);

        return allAllowedPositions;
    }

    private static IReadOnlyCollection<ListPosition> CollectParentAllowedListPositions(Type messageType)
    {
        var parentType = GetParentMessageType(messageType);
        return parentType is null ? [] : GetAllowedListPositions(parentType);
    }

    private static Type GetParentMessageType(Type messageType) =>
        messageType.GetCustomAttribute<ChildMessageAttribute>()?.ParentMessageType;
}
