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

        Text.LineBreak().LineBreak()
            .InlineUrl("Перейти к вишу", DeepLink.BuildWish(notificationSource.SubscribeId[..20], wish.WishId))
            .LineBreak()
            .InlineUrl("В главное меню", DeepLink.BuildMainMenu());

        return Task.CompletedTask;
    }
}
