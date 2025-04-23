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
    private readonly int _oldWishId;

    public DeleteWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish oldWish) : base(logger)
    {
        _notificationSource = notificationSource;
        _oldWish = oldWish;
    }

    public DeleteWishNotificationMessage(ILogger logger, int notificationSourceId, int wishId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
        _oldWishId = wishId;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);
        var oldWish = userContext.Wishes.First(w => w.WishId == _oldWishId);

        Keyboard
            .AddButton<SubscriptionQuery>("Перейти к подписке",
                    QueryParameter.ReadOnly,
                    new QueryParameter(QueryParameterType.SetUserTo, notificationSource.UserId))
            .NewRow()
            .AddButton<MainMenuQuery>("В главное меню");

        Text
            .InlineMention(notificationSource)
            .Italic(" удалил виш '")
            .ItalicBold(oldWish.Name)
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
