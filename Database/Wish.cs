using Telegram.Bot.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WishlistBot.Database;

public class Wish : BasePropertyChanged
{
   private string _name;
   private string _description;
   private string _fileId;

   public string Name 
   {
      get => _name;
      set => Set(ref _name, value);
   }

   public string Description
   {
      get => _description;
      set => Set(ref _description, value);
   }

   public string FileId
   {
      get => _fileId;
      set => Set(ref _fileId, value);
   }

   [JsonInclude]
   [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
   public ObservableCollection<string> Links { get; } = new();

   [JsonConstructor]
   public Wish()
   {
      Links.CollectionChanged += OnLinksCollectionChanged;
   }

   private void OnLinksCollectionChanged(object sender, NotifyCollectionChangedEventArgs ea)
   {
      RaisePropertyChanged(nameof(Links));
   }
}

