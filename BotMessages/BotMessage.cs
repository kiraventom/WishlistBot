using Serilog;
using System.Reflection;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries.Parameters;
using WishlistBot.Keyboard;
using WishlistBot.Database.Users;
using WishlistBot.Text;

namespace WishlistBot.BotMessages;

public abstract class BotMessage(ILogger logger)
{
   private bool _isInited;

   protected ILogger Logger { get; } = logger;

   public MessageText Text { get; } = new();
   public BotKeyboard Keyboard { get; } = new();
   public string PhotoFileId { get; protected set; }

   public async Task Init(BotUser user)
   {
      if (_isInited)
         return;

      if (!QueryParameterCollection.TryParse(user.QueryParams, out var parameters))
         parameters = new QueryParameterCollection();

      user.BotState = BotState.Default;

      var allowedTypes = GetAllowedTypes();

      Logger.Information($"unfiltered parameters: {parameters.ToString()}");
      FilterParameters(parameters, allowedTypes);
      Logger.Information($"allowed types for {this.GetType().Name}: {string.Join(", ", allowedTypes.Select(t => t.ToString()))}");
      Logger.Information($"filtered parameters: {parameters.ToString()}");

      Keyboard.InitCommonParameters(parameters);

      try
      {
         await InitInternal(user, parameters);
      }
      catch (Exception e)
      {
         Logger.Fatal(e.ToString());
         return;
      }

      // Parameters can change during message initialization
      user.QueryParams = parameters.ToString();

      _isInited = true;
   }

   protected abstract Task InitInternal(BotUser user, QueryParameterCollection parameters);

   private static void FilterParameters(QueryParameterCollection parameters, IReadOnlyCollection<QueryParameterType> allowedTypes)
   {
      var disallowedTypes = parameters
         .Select(p => p.Type)
         .Except(allowedTypes);

      foreach (var disallowedType in disallowedTypes)
         parameters.Pop(disallowedType);
   }

#pragma warning disable CA1859

   private IReadOnlyCollection<QueryParameterType> GetAllowedTypes() => GetAllowedTypes(GetType());

   private static IReadOnlyCollection<QueryParameterType> GetAllowedTypes(Type type)
   {
      var parentAllowedTypes = GetParentAllowedTypes(type);

      var allAllowedTypes = new List<QueryParameterType>(parentAllowedTypes);
      var allowedTypesAttributes = type.GetCustomAttributes<AllowedTypesAttribute>(inherit: true);
      foreach (var allowedTypesAttribute in allowedTypesAttributes)
      {
         var allowedTypes = allowedTypesAttribute.AllowedTypes;
         allAllowedTypes.AddRange(allowedTypes);
      }

      return allAllowedTypes;
   }

   private static IReadOnlyCollection<QueryParameterType> GetParentAllowedTypes(Type type)
   {
      var childMessageAttribute = type.GetCustomAttribute<ChildMessageAttribute>(inherit: true);
      if (childMessageAttribute is null)
         return [];

      var parentType = childMessageAttribute.ParentMessageType;
      return GetAllowedTypes(parentType);
   }
}
