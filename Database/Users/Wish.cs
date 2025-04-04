using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WishlistBot.Database.Users;

public class Wish : BasePropertyChanged
{
   private long _id;
   private string _name;
   private string _description;
   private string _fileId;
   private long _claimerId;
   private Price _priceRange;

   public long Id
   {
      get => _id;
      set => Set(ref _id, value);
   }

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

   public long ClaimerId
   {
      get => _claimerId;
      set => Set(ref _claimerId, value);
   }

   public Price PriceRange
   {
      get => _priceRange;
      set => Set(ref _priceRange, value);
   }

   [JsonInclude]
   [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
   public ObservableCollection<string> Links { get; } = [];

   [JsonConstructor]
   public Wish()
   {
      Links.CollectionChanged += OnLinksCollectionChanged;
   }

   // TODO Remove argument
   public Wish Clone(long newId)
   {
      var clone = new Wish
      {
         Id = newId,
         Name = Name,
         Description = Description,
         FileId = FileId,
         ClaimerId = ClaimerId,
         PriceRange = PriceRange
      };

      foreach (var link in Links)
         clone.Links.Add(link);

      return clone;
   }

   private void OnLinksCollectionChanged(object sender, NotifyCollectionChangedEventArgs ea) => RaisePropertyChanged(nameof(Links));
}
