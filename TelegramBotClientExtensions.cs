using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Serilog;

namespace WishlistBot;

public static class TelegramBotClientExtensions
{
   public static async Task ReplyToMessageAsync(this ITelegramBotClient client, ILogger logger, long chatId, string text, int messageToReplyToId)
   {
      var replyParameters = new ReplyParameters { MessageId = messageToReplyToId };

      await client.SendTextMessageAsync(chatId, text, null, ParseMode.None, null, null, false, false, null, replyParameters);
      logger.Information("Responded to [{messageId}] with '{text}'", messageToReplyToId, text);
   }
}
