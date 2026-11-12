using Serilog;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Keyboard;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ForceNewMessage)]
public abstract class BotMessage(ILogger logger)
{
    private bool _isInited;

    protected ILogger Logger { get; } = logger;

    public MessageText Text { get; } = new();
    public BotKeyboard Keyboard { get; } = new();

    /// <summary>
    /// Короткий тост, который показывается вместо пустого ответа на нажатие кнопки
    /// (всплывающая плашка сверху, без кнопки и без отдельного экрана).
    /// </summary>
    public string ToastText { get; protected set; }

    public bool ForceNewMessage { get; private set; }

    public async Task Init(UserContext userContext, UserModel user)
    {
        if (_isInited)
            return;

        // Уведомление живёт своей жизнью: оно не должно сбрасывать состояние экрана,
        // в котором пользователь сейчас находится (ввод, параметры, разрешённые кнопки,
        // страницы списков). Иначе одно уведомление ломает текущий экран.
        var isNotification = this is INotificationMessage;

        QueryParameterCollection parameters;
        if (isNotification || !QueryParameterCollection.TryParse(user.QueryParams, out parameters))
            parameters = new QueryParameterCollection();

        if (!isNotification)
            user.BotState = BotState.Default;

        FilterParameters(parameters, AllowedTypes);

        if (!isNotification)
            FilterPositions(userContext, user);

        if (parameters.Pop(QueryParameterType.ForceNewMessage))
            ForceNewMessage = true;

        Keyboard.InitCommonParameters(parameters);

        await InitInternal(userContext, user.UserId, parameters);

        if (isNotification)
        {
            // Кнопки уведомления (например, «В главное меню») должны работать, но не должны
            // вытеснять кнопки текущего экрана.
            var notificationQueries = Keyboard.EnumerateQueries().ToList();
            if (notificationQueries.Count != 0)
            {
                var allowed = string.IsNullOrEmpty(user.AllowedQueries)
                    ? notificationQueries
                    : user.AllowedQueries.Split(';').Concat(notificationQueries).Distinct().ToList();

                user.AllowedQueries = string.Join(';', allowed);
            }

            _isInited = true;
            return;
        }

        // Parameters can change during message initialization
        user.QueryParams = parameters.ToString();
        user.AllowedQueries = string.Join(';', Keyboard.EnumerateQueries());

        _isInited = true;
    }
    
    protected void DeleteWish(UserContext userContext, UserModel user, WishModel wishToDelete)
    {
        // Уведомления про этот виш больше не актуальны: выбрасываем и записи-источники,
        // и всё, что по ним уже лежит в очереди на отправку
        var notifications = userContext.Notifications
            .Where(n => n.SubjectId == wishToDelete.WishId)
            .ToList();

        foreach (var notification in notifications)
        {
            var pending = userContext.PendingNotifications
                .Where(p => p.NotificationId == notification.NotificationId)
                .ToList();

            userContext.PendingNotifications.RemoveRange(pending);
            userContext.Notifications.Remove(notification);
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
        // Строки-спутники грузит тот, кто достал пользователя: GetOrAddUser в контроллере,
        // SendOrEditBotMessage — для экранов. Если какой-то вызов забыл Include, дочитываем
        // здесь (без второго запроса, когда всё уже загружено), чтобы страницы списков
        // не перестали сбрасываться молча.
        var listPositionsReference = userContext.Entry(user).Reference(u => u.ListPositions);
        if (!listPositionsReference.IsLoaded)
            listPositionsReference.Load();

        if (user.ListPositions is null)
        {
            Logger.Warning("User [{userId}] list positions is null", user.UserId);
            return;
        }

        var listPositions = user.ListPositions;

        EnsureListPosition(userContext, listPositions, ListPosition.Friend);
        EnsureListPosition(userContext, listPositions, ListPosition.Claim);
        EnsureListPosition(userContext, listPositions, ListPosition.AdminBroadcast);
        EnsureListPosition(userContext, listPositions, ListPosition.AdminUserPage);

        foreach (var listPosition in Enum.GetValues<ListPosition>().Except(AllowedPositions))
            listPositions.GetListPosition(listPosition).Page = 0;
    }

    private static void EnsureListPosition(UserContext userContext, ListPositionsModel listPositions, ListPosition position)
    {
        switch (position)
        {
            case ListPosition.Friend: EnsureReference(userContext, listPositions, l => l.FriendPage); break;
            case ListPosition.Claim: EnsureReference(userContext, listPositions, l => l.ClaimPage); break;
            case ListPosition.AdminBroadcast: EnsureReference(userContext, listPositions, l => l.AdminBroadcastPage); break;
            case ListPosition.AdminUserPage: EnsureReference(userContext, listPositions, l => l.AdminUserPage); break;
            default: throw new NotSupportedException();
        }
    }

    // Rows created before the AdjustListPosition migration have NULL foreign keys, so the navigation
    // is an untracked instance left by the property initializer: all writes to it are silently lost.
    // Attach a real row instead, so that the page finally starts to persist for those users.
    private static void EnsureReference(
        UserContext userContext,
        ListPositionsModel listPositions,
        Expression<Func<ListPositionsModel, ListPositionModel>> navigation)
    {
        var reference = userContext.Entry(listPositions).Reference(navigation);
        reference.Load();

        if (reference.CurrentValue is null || userContext.Entry(reference.CurrentValue).State == EntityState.Detached)
            reference.CurrentValue = new ListPositionModel();
    }

    private IReadOnlyCollection<QueryParameterType> AllowedTypes => MessageMetadata.GetAllowedTypes(GetType());
    private IReadOnlyCollection<ListPosition> AllowedPositions => MessageMetadata.GetAllowedListPositions(GetType());
}
