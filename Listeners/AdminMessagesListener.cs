using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Admin.Broadcasts;
using WishlistBot.Database;
using WishlistBot.Database.Admin;

namespace WishlistBot.Listeners;

public class AdminMessagesListener(ILogger logger, ITelegramBotClient client, BroadcastsDb broadcastsDb) : IListener
{
   public async Task<bool> HandleMessageAsync(Message message, BotUser user)
   {
      switch (user.BotState)
      {
         case BotState.ListenForBroadcast:
            await HandleBroadcastAsync(message, user);
            break;

         default:
            return false;
      }
      
      return true;
   }

   private async Task HandleBroadcastAsync(Message message, BotUser user)
   {
      var text = message.Text ?? message.Caption;
      var fileId = message.Photo?.FirstOrDefault()?.FileId;

      if (string.IsNullOrEmpty(text))
      {
         logger.Warning("Received no text, ignoring");
         return;
      }

      if (message.MediaGroupId is not null)
         logger.Warning("Media groups are not supported, ignoring other photos");

      var broadcast = new Broadcast(GenerateBroadcastId(), text, fileId);
      await SendFinishAddBroadcastMessage(user, broadcast);
   }

   private async Task SendFinishAddBroadcastMessage(BotUser user, Broadcast broadcast)
   {
      var message = new FinishAddBroadcastMessage(logger, broadcastsDb, broadcast);
      await client.SendOrEditBotMessage(logger, user, message, forceNewMessage: true);
   }

   private long GenerateBroadcastId()
   {
      var ids = broadcastsDb.Values.Values.Select(b => b.Id).ToList();
      return DatabaseUtils.GenerateId(ids);
   }
}
