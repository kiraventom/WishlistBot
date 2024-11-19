using System.Text;
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
   private const char PARAM_SEPARATOR = ':';

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
      ParseQueryStr(actionText, out _, out var parameters);

      await Client.AnswerCallbackQuery(user.LastQueryId);
      user.LastQueryId = null;

      var message = MessageFactory.Build(_query, user, parameters);
      await Client.SendOrEditBotMessage(Logger, user, message);
   }

   public override bool IsMatch(string actionText)
   {
      var didParse = ParseQueryStr(actionText, out var name, out _);
      return didParse && base.IsMatch(name);
   }

   public static string BuildQueryStr(IQuery query, IReadOnlyCollection<string> parameters)
   {
      var stringBuilder = new StringBuilder();
      stringBuilder.Append(query.Data);

      foreach (var parameter in parameters)
      {
         stringBuilder.Append(PARAM_SEPARATOR);
         stringBuilder.Append(parameter);
      }

      return stringBuilder.ToString();
   }

   private static bool ParseQueryStr(string queryStr, out string name, out IReadOnlyCollection<string> parameters)
   {
      var parts = queryStr.Split(PARAM_SEPARATOR, 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      
      if (parts.Length > 1)
      {
         name = parts[0];
         parameters = parts[1..];
         return true;
      }

      if (parts.Length == 1)
      {
         name = parts[0];
         parameters = null;
         return true;
      }

      name = null;
      parameters = null;
      return false;
   }
}
