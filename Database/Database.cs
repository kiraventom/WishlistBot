using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WishlistBot.Database;

public abstract class Database<TKey, TValue>
{
   private static readonly object _locker = new();

   protected static readonly JsonSerializerOptions _options = new()
   {
      WriteIndented = true, AllowTrailingCommas = true
   };

   protected readonly ILogger _logger;
   protected readonly string _filePath;

   protected readonly Dictionary<TKey, TValue> _values;

   protected abstract string DatabaseName { get; }

   public IReadOnlyDictionary<TKey, TValue> Values => _values;

   protected Database(ILogger logger, string filepath, IReadOnlyDictionary<TKey, TValue> values)
   {
      _logger = logger;
      _filePath = filepath;
      _values = new Dictionary<TKey, TValue>(values);

      _logger.Information("{DatabaseName} DB loaded from '{filepath}', {valuesCount} values", 
            DatabaseName, filepath, _values.Count);
   }

   protected static Dictionary<TKey, TValue> LoadValues(ILogger logger, string filePath, string dbName)
   {
      if (!File.Exists(filePath))
      {
         logger.Warning("File '{filePath}' not found, creating empty {dbName} DB", filePath, dbName);

         var emptyValues = new Dictionary<TKey, TValue>();
         SaveTo(logger, filePath, dbName, emptyValues);
      }

      try
      {
         using var databaseFile = File.OpenRead(filePath);
         var values = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(databaseFile, _options);
         return values;
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
      }

      return null;
   }

   public void Save() => SaveTo(_logger, _filePath, DatabaseName, _values);

   protected static void SaveTo(ILogger logger, string filePath, string dbName, Dictionary<TKey, TValue> values)
   {
      lock (_locker)
      {
         using var file = File.Create(filePath);
         JsonSerializer.Serialize<Dictionary<TKey, TValue>>(file, values, _options);

         logger.Debug("{dbName} DB saved to '{filePath}', {valuesCount} values", dbName, filePath, values.Count);
      }
   }

}
