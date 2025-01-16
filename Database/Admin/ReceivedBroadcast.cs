using System.Text.Json.Serialization;

namespace WishlistBot.Database.Admin;

[method: JsonConstructor]
public readonly record struct ReceivedBroadcast(long BroadcastId, int MessageId);
