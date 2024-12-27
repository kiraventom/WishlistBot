using System.Text.Json.Serialization;

namespace WishlistBot.Database.Admin;

public readonly struct ReceivedBroadcast
{
   public long BroadcastId { get; }
   public int MessageId { get; }

   [JsonConstructor]
   public ReceivedBroadcast(long broadcastId, int messageId)
   {
      BroadcastId = broadcastId;
      MessageId = messageId;
   }
}
