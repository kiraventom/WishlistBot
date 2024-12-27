using Serilog;
using Telegram.Bot;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Notification;
using WishlistBot.Jobs;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmDeleteBroadcastMessage))]
public class DeleteBroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<BroadcastsQuery>("Back to broadcasts");

      parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

      var broadcastToDelete = broadcastsDb.Values[broadcastId];
      broadcastToDelete.Deleted = true;

      JobManager.Instance.StartJob("Delete broadcast", broadcastToDelete, Users, TimeSpan.FromSeconds(1), DeleteBroadcast);

      Text.Italic("Broadcast deletion started");
      return Task.CompletedTask;
   }

   private static async Task DeleteBroadcast(ILogger logger, ITelegramBotClient client, UsersDb _usersDb, BotUser recepient, Broadcast deletedBroadcast)
   {
      var didReceiveBroadcast = recepient.ReceivedBroadcasts.Any(b => b.BroadcastId == deletedBroadcast.Id);
      if (!didReceiveBroadcast)
         return;

      var receivedBroadcast = recepient.ReceivedBroadcasts.First(b => b.BroadcastId == deletedBroadcast.Id);
      var broadcastMessageId = receivedBroadcast.MessageId;

      try
      {
         await client.DeleteMessage(chatId: recepient.SenderId, messageId: broadcastMessageId);

         recepient.ReceivedBroadcasts.Remove(receivedBroadcast);
         logger.Information("Deleted broadcast [{bId}] from [{uId}]", deletedBroadcast.Id, recepient.SenderId);
      }
      catch (Exception)
      {
         logger.Information("Failed to delete broadcast message [{messageId}], looks like it was sent more than 48 hours ago", broadcastMessageId);
      }
   }
}
