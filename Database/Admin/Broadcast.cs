using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WishlistBot.Database.Admin;

public class Broadcast : BasePropertyChanged
{
   private long _id;
   private string _text;
   private string _fileId;
   private DateTime _dateTimeSent;
   private bool _deleted;

   [JsonInclude]
   public long Id
   {
      get => _id;
      set => Set(ref _id, value);
   }

   [JsonInclude]
   public string Text
   {
      get => _text;
      set => Set(ref _text, value);
   }

   [JsonInclude]
   public string FileId
   {
      get => _fileId;
      set => Set(ref _fileId, value);
   }

   [JsonInclude]
   public DateTime DateTimeSent
   {
      get => _dateTimeSent;
      set => Set(ref _dateTimeSent, value);
   }

   [JsonInclude]
   public bool Deleted
   {
      get => _deleted;
      set => Set(ref _deleted, value);
   }

   [JsonIgnore]
   public bool IsSent => DateTimeSent != DateTime.MinValue;

   [JsonConstructor]
   public Broadcast()
   {
   }

   public Broadcast(long id, string text, string fileId) : this()
   {
      Id = id;
      Text = text;
      FileId = fileId;
      DateTimeSent = DateTime.MinValue;
   }

   public string GetShortText()
   {
      if (Text is null)
         return "<empty>";

      return Text.Length <= 20
         ? Text
         : Text[..19] + "…";
   }
}
