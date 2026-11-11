using Serilog;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Notification;

/// <summary>Друзьям: владелец сделал виш постоянным.</summary>
public class WishPermanentNotificationMessage : BotMessage, INotificationMessage
{
    private readonly int _notificationSourceId;
    private readonly int _wishId;

    public WishPermanentNotificationMessage(ILogger logger, int notificationSourceId, int wishId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _wishId = wishId;
    }

    public WishPermanentNotificationMessage(ILogger logger, NotificationModel notificationModel) : base(logger)
    {
        _notificationSourceId = notificationModel.SourceId;
        _wishId = notificationModel.SubjectId.Value;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);
        var wish = userContext.Wishes.First(w => w.WishId == _wishId);

        Text
           .InlineMention(notificationSource)
           .Italic(" сделал виш ")
           .ItalicBold(wish.Name)
           .Italic(" постоянным!");

        Text.AddNotificationsSettingsLink();

        Keyboard
           .AddButton<ShowWishQuery>("Перейти к вишу",
                                     new QueryParameter(QueryParameterType.UserId, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.WishId, wish.WishId))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }
}
