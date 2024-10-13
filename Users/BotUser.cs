using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace WishlistBot.Users;

public class BotUser
{
   private long _senderId;
   private string _firstName;
   private BotState _botState;
   private string _lastQueryId;
   private int _lastBotMessageId = -1;

   [JsonInclude]
   public long SenderId
   {
      get => _senderId;
      set => Set(ref _senderId, value);
   }

   [JsonInclude]
   public string FirstName
   {
      get => _firstName;
      set => Set(ref _firstName, value);
   }

   [JsonInclude]
   public BotState BotState
   {
      get => _botState;
      set => Set(ref _botState, value);
   }

   [JsonInclude]
   public string LastQueryId
   {
      get => _lastQueryId;
      set => Set(ref _lastQueryId, value);
   }

   [JsonInclude]
   public int LastBotMessageId
   {
      get => _lastBotMessageId;
      set => Set(ref _lastBotMessageId, value);
   }

   public event Action<BotUser, string> PropertyChanged;

   [JsonConstructor]
   public BotUser()
   {
   }

   public BotUser(long senderId, string firstName)
   {
      SenderId = senderId;
      FirstName = firstName;
   }

   private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
   {
      if (!Equals(field, value))
      {
         field = value;
         PropertyChanged?.Invoke(this, propertyName);
      }
   }
}
