using Serilog;
using WishlistBot.Queries.Parameters;
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

      if (!QueryParameterCollection.TryParse(user.QueryParams, out var parameters))
         parameters = new QueryParameterCollection();

      try
      {
         InitInternal(user, parameters);
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

   protected abstract void InitInternal(BotUser user, QueryParameterCollection parameters);
}
