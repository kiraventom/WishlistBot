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
   public static async Task<Message> SendOrEditBotMessage(this ITelegramBotClient client, ILogger logger, BotUser user, BotMessage botMessage, bool forceNewMessage = false)
   {
      Message message;

      try
      {
         botMessage.Init(user);

         var text = botMessage.Text;
         var keyboardMarkup = botMessage.Keyboard.ToInlineKeyboardMarkup();
         var photoFileId = botMessage.PhotoFileId;

         if (user.LastBotMessageId < 0 || forceNewMessage)
         {
            if (photoFileId is null)
            {
               message = await client.SendMessage(chatId: user.SenderId, text: text, replyMarkup: keyboardMarkup);
            }
            else
            {
               var photo = InputFile.FromFileId(photoFileId);
               message = await client.SendPhoto(chatId: user.SenderId, photo: photo, caption: text, replyMarkup: keyboardMarkup);
            }

            logger.Information("Sent '{text}' to [{id}] with inline keyboard", text, user.SenderId);
         }
         else
         {
            if (photoFileId == null)
            {
               try
               {
                  message = await client.EditMessageText(chatId: user.SenderId, messageId: user.LastBotMessageId, text: text, replyMarkup: keyboardMarkup);
               }
               catch (Exception) // Message contains media
               {
                  logger.Warning("Failed to edit text of message [{messageId}], looks like it contains media", user.LastBotMessageId);

                  try
                  {
                     await client.DeleteMessage(chatId: user.SenderId, messageId: user.LastBotMessageId);
                  }
                  catch (Exception)
                  {
                     logger.Warning("Failed to delete message [{messageId}], looks like it was sent more than 48 hours ago", user.LastBotMessageId);
                  }

                  message = await client.SendMessage(chatId: user.SenderId, text: text, replyMarkup: keyboardMarkup);
               }
            }
            else
            {
               var photo = new InputMediaPhoto(InputFile.FromFileId(photoFileId));
               message = await client.EditMessageMedia(chatId: user.SenderId, messageId: user.LastBotMessageId, media: photo, replyMarkup: keyboardMarkup);
               message = await client.EditMessageCaption(chatId: user.SenderId, messageId: user.LastBotMessageId, caption: text, replyMarkup: keyboardMarkup);
            }

            logger.Information("Edited [{messageId}] to '{text}'", user.LastBotMessageId, text);
         }

         user.LastBotMessageId = message.MessageId;
      }
      catch (Exception e)
      {
         logger.Fatal(e.ToString());
         return null;
      }

      return message;
   }
}
