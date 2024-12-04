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

   public BotUser GetOrAddUser(long senderId, string firstName, string tag)
   {
      if (Dict.TryGetValue(senderId, out var existingUser))
      {
         if (existingUser.FirstName != firstName)
            existingUser.FirstName = firstName;

         if (existingUser.Tag != tag)
            existingUser.Tag = tag;

         return existingUser;
      }

      var user = new BotUser(senderId, firstName, tag);
      Dict.Add(senderId, user);

      Logger.Information("New user '{firstName}' [{senderId}] added to DB", firstName, senderId);

      user.PropertyChanged += OnUserPropertyChanged;
      Save();

      return user;
   }

   public bool RemoveUser(long senderId)
   {
      var contains = Dict.ContainsKey(senderId);
      if (contains)
      {
         var user = Dict[senderId];
         user.PropertyChanged -= OnUserPropertyChanged;
         Dict.Remove(senderId);

         Logger.Information("BotUser [{senderId}] removed from DB", senderId);
         Save();
      }
      else
      {
         Logger.Warning("Attempt to remove non-existent user [{senderId}]", senderId);
      }

      return contains;
   }

   public static UsersDb Load(ILogger logger, string filePath)
   {
      var values = LoadValues(logger, filePath, UsersDatabaseName);
      return values is null ? null : new UsersDb(logger, filePath, values);
   }

   private void OnUserPropertyChanged(BasePropertyChanged item, string propertyName)
   {
      if (item is BotUser user)
         Logger.Debug("Property {propertyName} of user [{senderId}] changed", propertyName, user.SenderId);

      Save();
   }
}
