using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.BotMessages.Notification;

public class DeleteWishNotificationMessage : BotMessage, INotificationMessage
{
    private readonly BotUser _notificationSource;
    private readonly Wish _oldWish;

    private readonly int _notificationSourceId;
    private readonly string _oldWishName;

    public DeleteWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish oldWish) : base(logger)
    {
        _notificationSource = notificationSource;
        _oldWish = oldWish;
    }

    public DeleteWishNotificationMessage(ILogger logger, int notificationSourceId, string oldWishName) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _oldWishName = oldWishName;
    }

    public DeleteWishNotificationMessage(ILogger logger, NotificationModel notificationModel) : base(logger)
    {
        _notificationSourceId = notificationModel.SourceId;
        _oldWishName = notificationModel.GetExtraString();
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);

        Keyboard
            .AddButton<SubscriptionQuery>("Перейти к подписке",
                    QueryParameter.ReadOnly,
                    new QueryParameter(QueryParameterType.SetUserTo, notificationSource.UserId))
            .NewRow()
            .AddButton<MainMenuQuery>("В главное меню");

        Text
            .InlineMention(notificationSource)
            .Italic(" удалил виш '")
            .ItalicBold(_oldWishName)
            .Italic("'!");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SubscriptionQuery>("Перейти к подписке",
                                         QueryParameter.ReadOnly,
                                         new QueryParameter(QueryParameterType.SetUserTo, _notificationSource.SenderId))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(_notificationSource)
           .Italic(" удалил виш '")
           .ItalicBold(_oldWish.Name)
           .Italic("'!");

        return Task.CompletedTask;
    }
}
