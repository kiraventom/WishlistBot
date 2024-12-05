using Serilog;
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
   public BotKeyboard Keyboard { get; protected set; }
   public string PhotoFileId { get; protected set; }

   public async Task Init(BotUser user)
   {
      if (_isInited)
         return;

      if (!QueryParameterCollection.TryParse(user.QueryParams, out var parameters))
         parameters = new QueryParameterCollection();

      user.BotState = BotState.Default;

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
}