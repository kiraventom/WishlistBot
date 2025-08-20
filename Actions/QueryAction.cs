using Telegram.Bot;
using Serilog;
using WishlistBot.Queries;
using WishlistBot.BotMessages;
using WishlistBot.Queries.Admin;
using WishlistBot.Model;

namespace WishlistBot.Actions;

public class QueryAction<T>(ILogger logger, ITelegramBotClient client, MessageFactory messageFactory)
   : UserAction(logger, client) where T : IQuery, new()
{
   private readonly IQuery _query = new T();

   private MessageFactory MessageFactory { get; } = messageFactory;

   public override string Name => _query.Data;

   public sealed override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
   {
      if (_query is IAdminQuery && !user.IsAdmin)
      {
         Logger.Warning("{query} sent, but [{id}] is not admin", Name, user.UserId);
         return;
      }

      QueryUtils.TryParseQueryStr(actionText, out _, out var parameters);

      var queryId = user.LastQueryId;
      user.LastQueryId = null;

      await Client.AnswerCallbackQuery(queryId);

      // We must pass parameters through DB, because sometimes we have to send message not after query action, but after message (see WishMessagesListener)
      user.QueryParams = parameters.ToString();

      var message = MessageFactory.Build(_query, userContext, queryId);
      await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, message);
   }

   public override bool IsMatch(string actionText)
   {
      var didParse = QueryUtils.TryParseQueryStr(actionText, out var name, out _);
      return didParse && base.IsMatch(name);
   }
}
