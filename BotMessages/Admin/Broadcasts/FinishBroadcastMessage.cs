using Serilog;
using Telegram.Bot;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Notification;
using WishlistBot.Jobs;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmBroadcastMessage))]
public class FinishBroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
   protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<BroadcastQuery>("Back to broadcast");

      parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

      var broadcastToSend = broadcastsDb.Values[broadcastId];
      broadcastToSend.DateTimeSent = DateTime.Now;

      Logger.Information("Started sending broadcast [{id}]", broadcastId);

      JobManager.Instance.StartJob("Send broadcast", broadcastToSend, Users, TimeSpan.FromSeconds(1), SendBroadcast);

      Text.Italic("Broadcast started");

      return Task.CompletedTask;
   }

   private static async Task SendBroadcast(ILogger logger, ITelegramBotClient client, UsersDb usersDb, BotUser recepient, Broadcast broadcast)
   {
      if (recepient.ReceivedBroadcasts.Any(b => b.BroadcastId == broadcast.Id))
         return;

      try
      {
         var broadcastNotification = new BroadcastNotificationMessage(logger, broadcast);
         var broadcastMessageId = await NotificationService.Instance.BroadcastToUser(broadcastNotification, recepient);

         recepient.ReceivedBroadcasts.Add(new ReceivedBroadcast(broadcast.Id, broadcastMessageId));

         logger.Information("Sent broadcast [{bId}] to [{uId}], messageId [{mId}]", broadcast.Id, recepient.SenderId, broadcastMessageId);
      }
      catch
      {
         logger.Error("Failed to sent broadcast to [{uId}]", recepient.SenderId);
      }
   }
}
