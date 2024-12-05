using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;

namespace WishlistBot.Actions.Commands;

public class StartCommand(ILogger logger, ITelegramBotClient client, UsersDb usersDb) : Command(logger, client)
{
   public override string Name => "/start";

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
      user.QueryParams = null;

      var isSubscribe = TryParseSubscribeId(actionText, out var subscribeId);
      if (isSubscribe)
      {
         // TODO [SQL] Fix this
         var userToSubscribeTo = usersDb.Values.Values.FirstOrDefault(u => u.SubscribeId == subscribeId);

         if (userToSubscribeTo is null)
            Logger.Warning("User with SubscribeId '{subscribeId}' was not found", subscribeId);

         await Client.SendOrEditBotMessage(Logger, user, new FinishSubscriptionMessage(Logger, userToSubscribeTo), forceNewMessage: true);
      }
      else
      {
         await Client.SendOrEditBotMessage(Logger, user, new MainMenuMessage(Logger), forceNewMessage: true);
      }
   }

   private static bool TryParseSubscribeId(string actionText, out string subscribeId)
   {
      var parts = actionText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 2)
      {
         subscribeId = parts[1];
         return true;
      }

      subscribeId = null;
      return false;
   }
}
