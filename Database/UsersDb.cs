using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WishlistBot.Database;

public class UsersDb
{
   private static readonly JsonSerializerOptions _options = new()
   {
      WriteIndented = true, AllowTrailingCommas = true
   };

   private static readonly object _locker = new();

   private readonly ILogger _logger;
   private readonly string _filePath;
   private readonly Dictionary<long, BotUser> _users;

   public IReadOnlyDictionary<long, BotUser> Users => _users;

   private UsersDb(ILogger logger, string filepath, IReadOnlyDictionary<long, BotUser> users)
   {
      _logger = logger;
      _filePath = filepath;
      _users = new Dictionary<long, BotUser>(users);

      _logger.Information("Users DB loaded from '{filepath}', {usersCount} users", filepath, _users.Count);
      
      foreach (var user in _users.Values)
      {
         user.PropertyChanged += OnUserPropertyChanged;
      }
   }

   public BotUser GetOrAddUser(long senderId, string firstName)
   {
      if (_users.ContainsKey(senderId))
         return _users[senderId];

      var user = new BotUser(senderId, firstName);
      _users.Add(senderId, user);

      _logger.Information("New user '{firstName}' [{senderId}] added to DB", firstName, senderId);

      user.PropertyChanged += OnUserPropertyChanged;
      Save();

      return user;
   }

   public bool RemoveUser(long senderId)
   {
      var contains = _users.ContainsKey(senderId);
      if (contains)
      {
         var user = _users[senderId];
         user.PropertyChanged -= OnUserPropertyChanged;
         _users.Remove(senderId);

         _logger.Information("BotUser [{senderId}] removed from DB", senderId);
         Save();
      }
      else
      {
         _logger.Warning("Attempt to remove non-existent user [{senderId}]", senderId);
      }

      return contains;
   }

   public static UsersDb Load(ILogger logger, string filePath)
   {
      if (!File.Exists(filePath))
      {
         logger.Warning("File '{filePath}' not found, creating empty users DB", filePath);

         var emptyUsers = new Dictionary<long, BotUser>();
         SaveTo(logger, filePath, emptyUsers);
      }

      UsersDb usersDb = null;

      try
      {
         using var usersDbFile = File.OpenRead(filePath);
         var users = JsonSerializer.Deserialize<Dictionary<long, BotUser>>(usersDbFile, _options);
         usersDb = new UsersDb(logger, filePath, users);
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
      }

      return usersDb;
   }

   public void Save() => SaveTo(_logger, _filePath, _users);

   private void OnUserPropertyChanged(BasePropertyChanged item, string propertyName)
   {
      if (item is BotUser user)
         _logger.Debug("Property {propertyName} of user [{senderId}] changed", propertyName, user.SenderId);

      Save();
   }

   private static void SaveTo(ILogger logger, string filePath, Dictionary<long, BotUser> users)
   {
      lock (_locker)
      {
         using var file = File.Create(filePath);
         JsonSerializer.Serialize<Dictionary<long, BotUser>>(file, users, _options);

         logger.Debug("Users DB saved to '{filePath}', {usersCount} users", filePath, users.Count);
      }
   }
}
