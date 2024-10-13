using Telegram.Bot;
using Serilog;
using WishlistBot;
using WishlistBot.Database;
using WishlistBot.Queries;
using WishlistBot.BotMessages;
using WishlistBot.Actions;

namespace WishlistBot.Actions;

public class QueryAction<T> : UserAction where T : IQuery, new()
{
   private readonly IQuery _query;

   private MessageFactory MessageFactory { get; }

   public override string Name => _query.Data;
   public string Caption => _query.Caption;

   public QueryAction(ILogger logger, ITelegramBotClient client, MessageFactory messageFactory) : base(logger, client)
   {
      _query = new T();
      MessageFactory = messageFactory;
   }

   public override async Task ExecuteAsync(BotUser user)
   {
      await Client.AnswerCallbackQueryAsync(user.LastQueryId);
      user.LastQueryId = null;

      var message = MessageFactory.Build(_query, user);
      await Client.SendOrEditBotMessageAsync(Logger, user, message);
   }
}
