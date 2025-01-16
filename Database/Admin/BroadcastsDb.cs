using Serilog;

namespace WishlistBot.Database.Admin;

public class BroadcastsDb : Database<long, Broadcast>
{
   private const string BroadcastsDatabaseName = "Broadcasts";
   protected override string DatabaseName => BroadcastsDatabaseName;

   private BroadcastsDb(ILogger logger, string filepath, IReadOnlyDictionary<long, Broadcast> broadcasts)
      : base(logger, filepath, broadcasts)
   {
   }

   public void Add(Broadcast broadcast)
   {
      Dict[broadcast.Id] = broadcast;
      broadcast.PropertyChanged += OnItemPropertyChanged;
      Save();
   }

   public void Remove(long id)
   {
      var contains = Dict.ContainsKey(id);
      if (contains)
      {
         var broadcast = Dict[id];
         broadcast.PropertyChanged -= OnItemPropertyChanged;
         Dict.Remove(id);
         Save();
      }
      else
      {
         Logger.Warning("Attempt to remove non-existent broadcast [{id}]", id);
      }
   }

   public static BroadcastsDb Load(ILogger logger, string filePath)
   {
      var values = LoadValues(logger, filePath, BroadcastsDatabaseName);
      return values is null ? null : new BroadcastsDb(logger, filePath, values);
   }
}
