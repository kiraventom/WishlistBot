using Serilog;
using WishlistBot.Queries;
using WishlistBot.Keyboard;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public abstract class BotMessage
{
   private bool _isInited;

   protected ILogger Logger { get; }

   public string Text { get; protected set; }
   public BotKeyboard Keyboard { get; protected set; }
   public string PhotoFileId { get; protected set; }

   protected BotMessage(ILogger logger)
   {
      Logger = logger;
   }

   public void Init(BotUser user)
   {
      if (_isInited)
         return;

      var lastQueryParams = user.LastQueryParams;
      if (!QueryParameter.TryParseQueryParams(lastQueryParams, out var parameters))
         parameters = Array.Empty<QueryParameter>();

      try
      {
         InitInternal(user, parameters);
      }
      catch (Exception e)
      {
         Logger.Fatal(e.ToString());
         return;
      }

      _isInited = true;
   }

   protected abstract void InitInternal(BotUser user, params QueryParameter[] parameters);

   protected static bool HasParameter(IReadOnlyCollection<QueryParameter> parameters, QueryParameterType type)
   {
      if (parameters is null)
         return false;

      return parameters.Any(p => p.Type == (byte)type);
   }

   protected static byte? GetParameter(IReadOnlyCollection<QueryParameter> parameters, QueryParameterType type)
   {
      if (!HasParameter(parameters, type)) // TODO Fix double check
         return null;

      return parameters.First(p => p.Type == (byte)type).Value;
   }
}
