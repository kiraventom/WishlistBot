using Telegram.Bot.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WishlistBot.Database;

public class Wish : BasePropertyChanged
{
   private string _name;

   [JsonInclude]
   public string Name 
   {
      get => _name;
      set => Set(ref _name, value);
   }

   [JsonInclude]
   [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
   public ObservableCollection<Message> Messages { get; } = new();

   [JsonConstructor]
   public Wish()
   {
      Messages.CollectionChanged += OnMessagesCollectionChanged;
   }

   private void OnMessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs ea)
   {
      RaisePropertyChanged(nameof(Messages));
   }
}
