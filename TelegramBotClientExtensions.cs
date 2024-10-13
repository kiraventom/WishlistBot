using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Database;

namespace WishlistBot;

public static class TelegramBotClientExtensions
{
   public static async Task<Message> SendOrEditBotMessageAsync(this ITelegramBotClient client, ILogger logger, BotUser user, BotMessage botMessage, bool forceNewMessage = false)
   {
      var text = botMessage.Text;
      var keyboardMarkup = botMessage.Keyboard.ToInlineKeyboardMarkup();

      Message message;
      if (user.LastBotMessageId < 0 || forceNewMessage)
      {
         message = await client.SendTextMessageAsync(user.SenderId, text, null, ParseMode.None, null, null, false, false, null, null, keyboardMarkup);
         logger.Information("Sent '{text}' to [{id}] with inline keyboard", text, user.SenderId);
      }
      else
      {
         message = await client.EditMessageTextAsync(user.SenderId, user.LastBotMessageId, text, ParseMode.None, null, null, keyboardMarkup);
         logger.Information("Edited [{messageId}] to '{text}'", user.LastBotMessageId, text);
      }
   
      user.LastBotMessageId = message.MessageId;
      return message;
   }
}
