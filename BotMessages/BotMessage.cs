using Serilog;
using System.Reflection;
using WishlistBot.Keyboard;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
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
}
