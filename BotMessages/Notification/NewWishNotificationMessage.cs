using Serilog;
using WishlistBot.Queries;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

public class NewWishNotificationMessage : BotMessage, INotificationMessage
{
    private readonly int _notificationSourceId;
    private readonly int _newWishId;

    public NewWishNotificationMessage(ILogger logger, int notificationSourceId, int newWishId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _newWishId = newWishId;
    }

    public NewWishNotificationMessage(ILogger logger, NotificationModel notificationModel) : base(logger)
    {
        _notificationSourceId = notificationModel.SourceId;
        _newWishId = notificationModel.SubjectId.Value;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == _notificationSourceId);
        var newWish = userContext.Wishes.First(w => w.WishId == _newWishId);

        var sender = userContext.Users.Include(u => u.WishViewSettings).First(u => u.UserId == userId);
        var wishViewSettings = sender.GetOrCreateWishViewSettings(_notificationSourceId);

        // TODO ToList() here is not very cool
        var wishes = notificationSource.GetSortedWishes(wishViewSettings).ToList();
        var wishIndex = wishes.IndexOf(newWish);

        // TODO: Make this prettier, DRY
        if (wishIndex == -1)
        {
            wishViewSettings.OnlyUnclaimed = false;
            wishes = notificationSource.GetSortedWishes(wishViewSettings).ToList();
            wishIndex = wishes.IndexOf(newWish);
        }

        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.UserId, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.WishId, newWish.WishId),
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
}
