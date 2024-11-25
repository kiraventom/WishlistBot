using System.Text;
using Telegram.Bot;
using Serilog;
using WishlistBot;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.BotMessages;
using WishlistBot.Actions;

namespace WishlistBot.Actions;

public class QueryAction<T> : UserAction where T : IQuery, new()
{
   private readonly IQuery _query;

   private MessageFactory MessageFactory { get; }

   public override string Name => _query.Data;
   public string Caption => _query.Caption;

   public QueryAction(ILogger logger, ITelegramBotClient client, MessageFactory messageFactory) 
      : base(logger, client)
   {
      _query = new T();
      MessageFactory = messageFactory;
   }

   public sealed override async Task ExecuteAsync(BotUser user, string actionText)
   {
      QueryUtils.TryParseQueryStr(actionText, out _, out var parameters);

      await Client.AnswerCallbackQuery(user.LastQueryId);
      user.LastQueryId = null;

      // We must pass parameters through DB, because sometimes we have to send message not after query action, butt after message (see WishMessagesListener)
      user.QueryParams = parameters.ToString();

      var message = MessageFactory.Build(_query, user);
      await Client.SendOrEditBotMessage(Logger, user, message);
   }

   public override bool IsMatch(string actionText)
   {
      var didParse = QueryUtils.TryParseQueryStr(actionText, out var name, out _);
      return didParse && base.IsMatch(name);
   }
}
