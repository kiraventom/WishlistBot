using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

public class BroadcastNotificationMessage(ILogger logger, Broadcast broadcast) : BotMessage(logger), INotificationMessage
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<MainMenuQuery>("В главное меню", QueryParameter.ForceNewMessage);

      Text.Bold("Рассылка от разработчика:")
         .LineBreak()
         .Italic(broadcast.Text);

      PhotoFileId = broadcast.FileId;

      return Task.CompletedTask;
   }
}
