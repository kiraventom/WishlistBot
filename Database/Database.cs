using Serilog;
using System.Text.Json;

namespace WishlistBot.Database;

public abstract class Database<TKey, TValue>
{
   private static readonly object _locker = new();

   private readonly string _filePath;

   protected readonly ILogger Logger;

   protected readonly Dictionary<TKey, TValue> Dict;

   protected abstract string DatabaseName { get; }

   public IReadOnlyDictionary<TKey, TValue> Values => Dict;

   protected Database(ILogger logger, string filepath, IReadOnlyDictionary<TKey, TValue> values)
   {
      Logger = logger;
      _filePath = filepath;
      Dict = new Dictionary<TKey, TValue>(values);

      Logger.Information("{DatabaseName} DB loaded from '{filepath}', {valuesCount} values",
                         DatabaseName, filepath, Dict.Count);

      foreach (var item in Dict.Values.OfType<BasePropertyChanged>())
      {
         item.PropertyChanged += OnItemPropertyChanged;
      }
   }

   protected static Dictionary<TKey, TValue> LoadValues(ILogger logger, string filePath, string dbName)
   {
      ArgumentNullException.ThrowIfNull(filePath);

      if (!File.Exists(filePath))
      {
         logger.Warning("File '{filePath}' not found, creating empty {dbName} DB", filePath, dbName);

         var emptyValues = new Dictionary<TKey, TValue>();
         SaveTo(logger, filePath, dbName, emptyValues);
      }

      try
      {
         using var databaseFile = File.OpenRead(filePath);
         var values = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(databaseFile, CommonOptions.Json);
         return values;
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
      }

      return null;
   }

   protected void Save() => SaveTo(Logger, _filePath, DatabaseName, Dict);

   protected void OnItemPropertyChanged(BasePropertyChanged item, string propertyName) => Save();

   private static void SaveTo(ILogger logger, string filePath, string dbName, Dictionary<TKey, TValue> values)
   {
      lock (_locker)
      {
         using var file = File.Create(filePath);
         JsonSerializer.Serialize(file, values, CommonOptions.Json);

         logger.Debug("{dbName} DB saved to '{filePath}', {valuesCount} values", dbName, filePath, values.Count);
      }
   }
}
