using Serilog;

namespace WishlistBot.Database.Users;

public class UsersDb : Database<long, BotUser>
{
   private const string UsersDatabaseName = "Users";
   protected override string DatabaseName => UsersDatabaseName;

   private UsersDb(ILogger logger, string filepath, IReadOnlyDictionary<long, BotUser> users)
      : base(logger, filepath, users)
   {
      foreach (var user in users.Values)
      {
         user.PropertyChanged += OnUserPropertyChanged;
      }
   }

   public BotUser GetOrAddUser(long senderId, string firstName)
   {
      if (_values.ContainsKey(senderId))
         return _values[senderId];

      var user = new BotUser(senderId, firstName);
      _values.Add(senderId, user);

      _logger.Information("New user '{firstName}' [{senderId}] added to DB", firstName, senderId);

      user.PropertyChanged += OnUserPropertyChanged;
      Save();

      return user;
   }

   public bool RemoveUser(long senderId)
   {
      var contains = _values.ContainsKey(senderId);
      if (contains)
      {
         var user = _values[senderId];
         user.PropertyChanged -= OnUserPropertyChanged;
         _values.Remove(senderId);

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
      var values = LoadValues(logger, filePath, UsersDatabaseName);
      if (values is null)
         return null;

      return new UsersDb(logger, filePath, values);
   }

   private void OnUserPropertyChanged(BasePropertyChanged item, string propertyName)
   {
      if (item is BotUser user)
         _logger.Debug("Property {propertyName} of user [{senderId}] changed", propertyName, user.SenderId);

      Save();
   }
}
