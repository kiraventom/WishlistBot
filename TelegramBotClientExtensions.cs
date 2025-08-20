using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Serilog;
using WishlistBot.BotMessages;

using WishlistBot.Model;
using WishlistBot.Notification;

namespace WishlistBot;

// TODO Move code to BotMessageSender; replace Client in BotMessage with BotMessageSender; make MediaStorageManager not singleton
public static class TelegramBotClientExtensions
{
    public static async Task<Message> SendOrEditBotMessage(this ITelegramBotClient client, ILogger logger, UserContext userContext, int userId, BotMessage botMessage, bool forceNewMessage = false)
    {
        var userModel = userContext.Users.First(u => u.UserId == userId);

        Message message;

        try
        {
            await botMessage.Init(userContext, userModel);

            if (botMessage.ForceNewMessage)
                forceNewMessage = true;
        }
        catch (Exception e)
        {
            if (botMessage is InvalidMessage)
            {
                logger.Fatal("Failed to initialize {invalidMessageName}, shit got real", nameof(InvalidMessage));
                return null;
            }
            else
            {
                logger.Fatal(e.ToString());

                // Do not sent invalid message as notification, it's useless and annoying
                if (botMessage is not INotificationMessage)
                {
                    var invalidMessage = new InvalidMessage(logger);
                    var sentMessage = await client.SendOrEditBotMessage(logger, userContext, userId, invalidMessage, forceNewMessage: true);
                    return sentMessage;
                }
                else
                {
                    return null;
                }
            }
        }

        try
        {
            var text = botMessage.Text.ToString();

            var keyboardMarkup = botMessage.Keyboard.ToInlineKeyboardMarkup();
            var photoFileId = botMessage.PhotoFileId;

            if (photoFileId != null)
            {
                await MediaStorageManager.Instance.Store(photoFileId);
            }

            var shouldSendNewMessage = userModel.LastBotMessageId < 0 || forceNewMessage || userModel.ReceivedBroadcasts.Any(b => b.MessageId == userModel.LastBotMessageId);

            if (shouldSendNewMessage)
            {
                if (photoFileId is null)
                {
                    message = await client.SendMessage(chatId: userModel.TelegramId, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: true);
                }
                else
                {
                    var photo = InputFile.FromFileId(photoFileId);
                    message = await client.SendPhoto(chatId: userModel.TelegramId, photo: photo, caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
                }

                logger.Information("Sent '{text}' to [{id}] with inline keyboard", text, userModel.UserId);
            }
            else
            {
                if (photoFileId == null)
                {
                    try
                    {
                        message = await client.EditMessageText(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: true);
                    }
                    catch (Exception) // Message contains media
                    {
                        logger.Information("Failed to edit text of message [{messageId}], looks like it contains media", userModel.LastBotMessageId);

                        message = await client.SendMessage(chatId: userModel.TelegramId, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: true);

                        try
                        {
                            await client.DeleteMessage(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value);
                        }
                        catch (Exception)
                        {
                            logger.Information("Failed to delete message [{messageId}], looks like it was sent more than 48 hours ago", userModel.LastBotMessageId);
                        }
                    }
                }
                else
                {
                    var photo = new InputMediaPhoto(InputFile.FromFileId(photoFileId));
                    await client.EditMessageMedia(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value, media: photo, replyMarkup: keyboardMarkup);
                    message = await client.EditMessageCaption(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value, caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
                }

                logger.Information("Edited [{messageId}] to '{text}'", userModel.LastBotMessageId, text);
            }

            userModel.LastBotMessageId = message.MessageId;
        }
        catch (Exception e)
        {
            logger.Fatal(e.ToString());
            return null;
        }

        return message;
    }
}
