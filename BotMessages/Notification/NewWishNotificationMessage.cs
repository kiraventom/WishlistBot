using Serilog;
using WishlistBot.Queries;
using WishlistBot.Notification;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

public class NewWishNotificationMessage : BotMessage, INotificationMessage
{
    private readonly BotUser _notificationSource;
    private readonly Wish _newWish;

    private readonly int _notificationSourceId;
    private readonly int _newWishId;

    public NewWishNotificationMessage(ILogger logger, int notificationSourceId, int newWishId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _newWishId = newWishId;
    }

    public NewWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish newWish) : base(logger)
    {
        _notificationSource = notificationSource;
        _newWish = newWish;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == _notificationSourceId);
        var newWish = userContext.Wishes.First(w => w.WishId == _newWishId);

        var wishIndex = notificationSource.Wishes.IndexOf(newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.SetUserTo, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.SetWishTo, newWish.WishId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(notificationSource)
           .Italic(" добавил новый виш '")
           .ItalicBold(newWish.Name)
           .Italic("'!");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var wishIndex = _notificationSource.Wishes.IndexOf(_newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.SetUserTo, _notificationSource.SenderId),
                                     new QueryParameter(QueryParameterType.SetWishTo, _newWish.Id),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(_notificationSource)
           .Italic(" добавил новый виш '")
           .ItalicBold(_newWish.Name)
           .Italic("'!");

        return Task.CompletedTask;
    }
}
