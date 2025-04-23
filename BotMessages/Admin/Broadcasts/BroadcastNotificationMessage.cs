using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

public class BroadcastNotificationMessage : BotMessage, INotificationMessage
{
    private readonly Broadcast _broadcast;
    private readonly int _broadcastId;

    public BroadcastNotificationMessage(ILogger logger, Broadcast broadcast) : base(logger)
    {
        _broadcast = broadcast;
    }

    public BroadcastNotificationMessage(ILogger logger, int broadcastId) : base(logger)
    {
        _broadcastId = broadcastId;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MainMenuQuery>("В главное меню", QueryParameter.ForceNewMessage);

        var broadcast = userContext.Broadcasts.AsNoTracking().First(b => b.BroadcastId == _broadcastId);
        Text.Bold("Рассылка от разработчика:")
           .LineBreak()
           .Italic(broadcast.Text);

        PhotoFileId = broadcast.FileId;

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MainMenuQuery>("В главное меню", QueryParameter.ForceNewMessage);

        Text.Bold("Рассылка от разработчика:")
           .LineBreak()
           .Italic(_broadcast.Text);

        PhotoFileId = _broadcast.FileId;

        return Task.CompletedTask;
    }
}
