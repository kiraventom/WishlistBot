using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WishlistBot.Database.Users;

public class BotUser : BasePropertyChanged
{
   private long _senderId;
   private string _firstName;
   private BotState _botState;
   private string _lastQueryId;
   private int _lastBotMessageId = -1;
   private string _queryParams;
   private Wish _currentWish;

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

   // TODO: store QueryParametersCollection here
   [JsonInclude]
   public string QueryParams
   {
      get => _queryParams;
      set => Set(ref _queryParams, value);
   }

   [JsonInclude]
   [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
   public ObservableCollection<Wish> Wishes { get; } = new();

   [JsonInclude]
   public Wish CurrentWish
   {
      get => _currentWish;
      set
      {
         if (_currentWish is not null)
            _currentWish.PropertyChanged -= OnWishPropertyChanged;

         if (value is not null)
            value.PropertyChanged += OnWishPropertyChanged;

         Set(ref _currentWish, value);
      }
   }

   [JsonConstructor]
   public BotUser()
   {
      Wishes.CollectionChanged += OnWishesCollectionChanged;
   }

   public BotUser(long senderId, string firstName) : this()
   {
      SenderId = senderId;
      FirstName = firstName;
   }

   private void OnWishesCollectionChanged(object sender, NotifyCollectionChangedEventArgs ea)
   {
      var oldWish = ea.OldItems?.OfType<Wish>()?.Single();
      if (oldWish is not null)
         oldWish.PropertyChanged -= OnWishPropertyChanged;

      var newWish = ea.NewItems?.OfType<Wish>()?.Single();
      if (newWish is not null)
         newWish.PropertyChanged += OnWishPropertyChanged;

      RaisePropertyChanged(nameof(Wishes));
   }

   private void OnWishPropertyChanged(BasePropertyChanged sender, string propertyName)
   {
      RaisePropertyChanged(nameof(Wishes));
   }
}
