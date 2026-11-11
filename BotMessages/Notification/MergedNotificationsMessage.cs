using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Несколько событий друзей, накопившихся за время активности, одним сообщением.
///
/// Внутри такого списка кнопку на каждый пункт не повесить, поэтому переходы — обычные
/// инлайн-ссылки: ссылкой служит название виша, а у «добавил в друзья» — имя друга.
/// </summary>
public class MergedNotificationsMessage(ILogger logger, IReadOnlyList<PendingNotificationModel> pending) : BotMessage(logger), INotificationMessage
{
    /// <summary>Сколько пунктов попало в сообщение (часть может отвалиться: виш уже удалён).</summary>
    public int ItemsCount => _items.Count;

    private readonly List<MergedItem> _items = [];

    /// <summary>Один пункт сводки: что показать и куда вести.</summary>
    private sealed record MergedItem(
        NotificationMessageType Type,
        UserModel Source,
        WishModel Wish,
        string OldWishName,
        WishPropertyType Changes);

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        _items.Clear();

        foreach (var item in pending)
        {
            if (Resolve(userContext, item) is { } resolved)
                _items.Add(resolved);
        }

        Text.Bold("\U0001f514 Уведомления").LineBreak().LineBreak();

        for (var i = 0; i < _items.Count; ++i)
        {
            AppendItem(_items[i]);

            if (i < _items.Count - 1)
                Text.LineBreak();
        }

        if (_items.Count == 0)
            return Task.CompletedTask;

        Text.AddNotificationsSettingsLink();

        Keyboard.AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }

    private static MergedItem Resolve(UserContext userContext, PendingNotificationModel pendingItem)
    {
        // «Добавил вас в друзья» не заводит запись-источник: автор лежит в payload
        if (pendingItem.Type == PendingNotificationType.NewFriend)
        {
            if (!int.TryParse(pendingItem.Payload, out var sourceUserId))
                return null;

            var friend = userContext.Users.FirstOrDefault(user => user.UserId == sourceUserId);

            return friend is null
                ? null
                : new MergedItem(NotificationMessageType.NewFriend, friend, null, null, WishPropertyType.None);
        }

        var notification = userContext.Notifications
            .Include(n => n.Source)
            .FirstOrDefault(n => n.NotificationId == pendingItem.NotificationId);

        if (notification?.Source is not { } source)
            return null;

        var wish = notification.SubjectId is { } wishId
            ? userContext.Wishes.FirstOrDefault(w => w.WishId == wishId)
            : null;

        // От виша зависит всё, кроме «удалил»: остальные пункты ведут на виш,
        // а если виша уже нет — пункту нечего показать
        var needsWish = notification.Type is NotificationMessageType.NewWish
            or NotificationMessageType.WishEdit
            or NotificationMessageType.WishPermanent;

        if (needsWish && wish is null)
            return null;

        var oldWishName = notification.GetExtraString();

        if (notification.Type == NotificationMessageType.WishDelete && string.IsNullOrEmpty(oldWishName))
            return null;

        return new MergedItem(notification.Type, source, wish, oldWishName, (WishPropertyType)notification.GetExtraInt());
    }

    private void AppendItem(MergedItem item)
    {
        var friendLink = DeepLink.BuildFriend(item.Source.SubscribeId[..20]);

        switch (item.Type)
        {
            case NotificationMessageType.NewFriend:
                Text.InlineUrl(item.Source.FirstName, friendLink)
                    .Italic(" добавил вас в друзья!");
                break;

            case NotificationMessageType.NewWish:
                Text.InlineMention(item.Source)
                    .Italic(" добавил новый виш '")
                    .InlineUrl(item.Wish.Name, BuildWishLink(item))
                    .Italic("'!");
                break;

            case NotificationMessageType.WishEdit:
                Text.InlineMention(item.Source)
                    .Italic(" изменил ")
                    .Italic(DescribeChanges(item.Changes))
                    .Italic(" у виша '")
                    .InlineUrl(item.Wish.Name, BuildWishLink(item))
                    .Italic("'!");
                break;

            case NotificationMessageType.WishDelete:
                // Виша больше нет, вести некуда: имя друга остаётся упоминанием
                Text.InlineMention(item.Source)
                    .Italic(" удалил виш '")
                    .ItalicBold(item.OldWishName)
                    .Italic("'!");
                break;

            case NotificationMessageType.WishPermanent:
                Text.InlineMention(item.Source)
                    .Italic(" сделал виш ")
                    .InlineUrl(item.Wish.Name, BuildWishLink(item))
                    .Italic(" постоянным!");
                break;
        }
    }

    private static string BuildWishLink(MergedItem item) =>
        DeepLink.BuildWish(item.Source.SubscribeId[..20], item.Wish.WishId);

    private static string DescribeChanges(WishPropertyType wishPropertyType)
    {
        var changedItemsNames = new List<string>();

        if (wishPropertyType.HasFlag(WishPropertyType.Name))
            changedItemsNames.Add("название");

        if (wishPropertyType.HasFlag(WishPropertyType.Description))
            changedItemsNames.Add("описание");

        if (wishPropertyType.HasFlag(WishPropertyType.Media))
            changedItemsNames.Add("фото");

        if (wishPropertyType.HasFlag(WishPropertyType.Links))
            changedItemsNames.Add("ссылки");

        return changedItemsNames.Count switch
        {
            0 => "виш",
            1 => changedItemsNames.Single(),
            _ => string.Join(", ", changedItemsNames.SkipLast(1)) + $" и {changedItemsNames.Last()}",
        };
    }
}
