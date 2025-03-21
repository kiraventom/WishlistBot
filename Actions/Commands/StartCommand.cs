using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.QueryParameters;

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
         {
            await Client.SendOrEditBotMessage(Logger, user, new FailSubscriptionMessage(Logger), forceNewMessage: true);
            return;
         }

         // TEMP
         if (userToSubscribeTo.SenderId == user.SenderId)
         {
            await Client.SendOrEditBotMessage(Logger, user, new MainMenuMessage(Logger), forceNewMessage: true);
            return;
         }

         var collection = new QueryParameterCollection([new QueryParameter(QueryParameterType.SetUserTo, userToSubscribeTo.SenderId)]);
         user.QueryParams = collection.ToString();
         await Client.SendOrEditBotMessage(Logger, user, new FinishSubscriptionMessage(Logger, usersDb), forceNewMessage: true);
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
