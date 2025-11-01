using Serilog;
using System.Reflection;
using WishlistBot.Keyboard;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using WishlistBot.Jobs;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ForceNewMessage)]
public abstract class BotMessage(ILogger logger)
{
    private bool _isInited;

    protected ILogger Logger { get; } = logger;

    public MessageText Text { get; } = new();
    public BotKeyboard Keyboard { get; } = new();
    public string PhotoFileId { get; protected set; }

    public bool ForceNewMessage { get; private set; }

    public async Task Init(UserContext userContext, UserModel user)
    {
        if (_isInited)
            return;

        if (!QueryParameterCollection.TryParse(user.QueryParams, out var parameters))
            parameters = new QueryParameterCollection();

        user.BotState = BotState.Default;

        FilterParameters(parameters, AllowedTypes);
        FilterPositions(userContext, user);

        if (parameters.Pop(QueryParameterType.ForceNewMessage))
            ForceNewMessage = true;

        Keyboard.InitCommonParameters(parameters);

        await InitInternal(userContext, user.UserId, parameters);

        // Parameters can change during message initialization
        user.QueryParams = parameters.ToString();
        user.AllowedQueries = string.Join(';', Keyboard.EnumerateQueries());

        _isInited = true;
    }
    
    protected void DeleteWish(UserContext userContext, UserModel user, WishModel wishToDelete)
    {
        var notification = userContext.Notifications.FirstOrDefault(n => n.SubjectId == wishToDelete.WishId);

        if (notification is not null)
        {
            JobManager.Instance.StopNotificationJob(notification.NotificationId);
        }

        user.Wishes.Remove(wishToDelete);
    }

    protected abstract Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters);

    private static void FilterParameters(QueryParameterCollection parameters, IReadOnlyCollection<QueryParameterType> allowedTypes)
    {
        var disallowedTypes = parameters.Select(p => p.Type).Except(allowedTypes);

        foreach (var disallowedType in disallowedTypes)
            parameters.Pop(disallowedType);
    }

    private void FilterPositions(UserContext userContext, UserModel user)
    {
        // TODO Bruh
        userContext.Entry(user).Reference(u => u.ListPositions).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.WishPage).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.SubscriberPage).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.SubscriptionPage).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.ClaimPage).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.AdminBroadcastPage).Load();
        userContext.Entry(user.ListPositions).Reference(u => u.AdminUserPage).Load();

        foreach (var listPosition in Enum.GetValues<ListPosition>().Except(AllowedPositions))
            user.ListPositions.GetListPosition(listPosition).Page = 0;
    }

#pragma warning disable CA1859

    private static Dictionary<Type, IReadOnlyCollection<QueryParameterType>> _allowedTypes = [];
    private IReadOnlyCollection<QueryParameterType> AllowedTypes
    {
        get
        {
            var type = GetType();
            if (_allowedTypes.ContainsKey(type))
                return _allowedTypes[type];
            else
                return _allowedTypes[type] = GetAllowedTypes(type);
        }
    }

    // TODO move all that into separate class
    private static Dictionary<Type, IReadOnlyCollection<ListPosition>> _allowedPositions = [];
    private IReadOnlyCollection<ListPosition> AllowedPositions
    {
        get
        {
            var type = GetType();
            if (_allowedPositions.ContainsKey(type))
                return _allowedPositions[type];
            else
                return _allowedPositions[type] = GetAllowedListPositions(type);
        }
    }

    private static IReadOnlyCollection<QueryParameterType> GetAllowedTypes(Type type)
    {
        var parentAllowedTypes = GetParentAllowedTypes(type);

        var allAllowedTypes = new HashSet<QueryParameterType>(parentAllowedTypes);
        var allowedTypesAttributes = type.GetCustomAttributes<AllowedTypesAttribute>();
        foreach (var allowedTypesAttribute in allowedTypesAttributes)
        {
            var allowedTypes = allowedTypesAttribute.AllowedTypes;
            foreach (var allowedType in allowedTypes)
                allAllowedTypes.Add(allowedType);
        }

        return allAllowedTypes;
    }

    private static IReadOnlyCollection<QueryParameterType> GetParentAllowedTypes(Type type)
    {
        var childMessageAttribute = type.GetCustomAttribute<ChildMessageAttribute>();
        if (childMessageAttribute is null)
            return [];

        var parentType = childMessageAttribute.ParentMessageType;
        return GetAllowedTypes(parentType);
    }

    private static IReadOnlyCollection<ListPosition> GetAllowedListPositions(Type type)
    {
        var parentListPositions = GetParentAllowedListPositions(type);

        var allAllowedPositions = new HashSet<ListPosition>(parentListPositions);
        var allowedPositionsAttributes = type.GetCustomAttributes<AllowedListPositionsAttribute>();
        foreach (var allowedPositionsAttribute in allowedPositionsAttributes)
        {
            var allowedPositions = allowedPositionsAttribute.AllowedPositions;
            foreach (var allowedPosition in allowedPositions)
                allAllowedPositions.Add(allowedPosition);
        }

        return allAllowedPositions;
    }

    private static IReadOnlyCollection<ListPosition> GetParentAllowedListPositions(Type type)
    {
        var childMessageAttribute = type.GetCustomAttribute<ChildMessageAttribute>();
        if (childMessageAttribute is null)
            return [];

        var parentType = childMessageAttribute.ParentMessageType;
        return GetAllowedListPositions(parentType);
    }
}
