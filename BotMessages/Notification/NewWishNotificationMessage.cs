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

        // Уведомление живёт в топике, где кнопки бесполезны — ведём ссылками в основной чат
        Text.LineBreak().LineBreak()
            .InlineUrl("Перейти к вишу", DeepLink.BuildWish(notificationSource.SubscribeId[..20], newWish.WishId))
            .LineBreak()
            .InlineUrl("В главное меню", DeepLink.BuildMainMenu());

        Text
           .InlineMention(notificationSource)
           .Italic(" добавил новый виш '")
           .ItalicBold(newWish.Name)
           .Italic("'!");

        Text.AddNotificationsSettingsLink();

        return Task.CompletedTask;
    }
}
