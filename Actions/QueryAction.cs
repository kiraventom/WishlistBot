using Telegram.Bot;
using Serilog;
using WishlistBot.Queries;
using WishlistBot.BotMessages;
using WishlistBot.Queries.Admin;
using WishlistBot.Model.User;

namespace WishlistBot.Actions;

public class QueryAction<T>(ILogger logger, BotMessageSender sender, MessageFactory messageFactory)
   : UserAction(logger, sender) where T : IQuery, new()
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

      // We must pass parameters through DB, because sometimes we have to send message not after query action, but after message (see WishMessagesListener)
      user.QueryParams = parameters.ToString();

      var message = MessageFactory.Build(_query, userContext, queryId);
      await Sender.SendOrEdit(userContext, user.UserId, message);

      // Отвечаем после сборки сообщения: экран может попросить короткий тост вместо пустого ответа
      await Sender.AnswerCallbackQuery(queryId, message?.ToastText);
   }

   public override bool IsMatch(string actionText)
   {
      var didParse = QueryUtils.TryParseQueryStr(actionText, out var name, out _);
      return didParse && base.IsMatch(name);
   }
}
