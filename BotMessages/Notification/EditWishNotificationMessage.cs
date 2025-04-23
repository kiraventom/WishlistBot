using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.BotMessages.Notification;

public class EditWishNotificationMessage : BotMessage, INotificationMessage
{
    private readonly BotUser _notificationSource;
    private readonly Wish _editedWish;

    private readonly int _notificationSourceId;
    private readonly int _editedWishId;

    private readonly WishPropertyType _wishPropertyType;

    public EditWishNotificationMessage(ILogger logger, int notificationSourceId, int editedWishId, WishPropertyType wishPropertyType) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _editedWishId = editedWishId;
        _wishPropertyType = wishPropertyType;
    }

    public EditWishNotificationMessage(ILogger logger, NotificationModel notificationModel) : base(logger)
    {
        _notificationSourceId = notificationModel.SourceId;
        _editedWishId = notificationModel.SubjectId.Value;
        _wishPropertyType = (WishPropertyType)notificationModel.GetExtraInt();
    }

    public EditWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish editedWish, WishPropertyType wishPropertyType) : base(logger)
    {
        _notificationSource = notificationSource;
        _editedWish = editedWish;
        _wishPropertyType = wishPropertyType;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);
        var editedWish = userContext.Wishes.First(w => w.WishId == _editedWishId);

        var wishIndex = notificationSource.GetSortedWishes().IndexOf(editedWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.SetUserTo, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.SetWishTo, editedWish.WishId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        var changedItemsNames = new List<string>();

        if (_wishPropertyType.HasFlag(WishPropertyType.Name))
            changedItemsNames.Add("название");

        if (_wishPropertyType.HasFlag(WishPropertyType.Description))
            changedItemsNames.Add("описание");

        if (_wishPropertyType.HasFlag(WishPropertyType.Media))
            changedItemsNames.Add("фото");

        if (_wishPropertyType.HasFlag(WishPropertyType.Links))
            changedItemsNames.Add("ссылки");

        var changedItemsText = changedItemsNames.Count switch
        {
            0 => throw new NotSupportedException($"WishPropertyType value '{_wishPropertyType}' is not supported"),
            1 => changedItemsNames.Single(),
            _ => string.Join(", ", changedItemsNames.SkipLast(1)) + $" и {changedItemsNames.Last()}",
        };

        Text
           .InlineMention(notificationSource)
           .Italic(" изменил ")
           .ItalicBold(changedItemsText)
           .Italic(" у виша '")
           .ItalicBold(editedWish.Name)
           .Italic("'!");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var wishIndex = _notificationSource.Wishes.IndexOf(_editedWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.SetUserTo, _notificationSource.SenderId),
                                     new QueryParameter(QueryParameterType.SetWishTo, _editedWish.Id),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        var changedItemsNames = new List<string>();

        if (_wishPropertyType.HasFlag(WishPropertyType.Name))
            changedItemsNames.Add("название");

        if (_wishPropertyType.HasFlag(WishPropertyType.Description))
            changedItemsNames.Add("описание");

        if (_wishPropertyType.HasFlag(WishPropertyType.Media))
            changedItemsNames.Add("фото");

        if (_wishPropertyType.HasFlag(WishPropertyType.Links))
            changedItemsNames.Add("ссылки");

        var changedItemsText = changedItemsNames.Count switch
        {
            0 => throw new NotSupportedException($"WishPropertyType value '{_wishPropertyType}' is not supported"),
            1 => changedItemsNames.Single(),
            _ => string.Join(", ", changedItemsNames.SkipLast(1)) + $" и {changedItemsNames.Last()}",
        };

        Text
           .InlineMention(_notificationSource)
           .Italic(" изменил ")
           .ItalicBold(changedItemsText)
           .Italic(" у виша '")
           .ItalicBold(_editedWish.Name)
           .Italic("'!");

        return Task.CompletedTask;
    }
}
