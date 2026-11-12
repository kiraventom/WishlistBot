using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Model.User;
using WishlistBot.Notification;

namespace WishlistBot;

/// <summary>
/// Отправка экранов бота: решает, править ли текущее сообщение пользователя или слать
/// новое, хранит <see cref="UserModel.LastBotMessageId"/>, убирает клавиатуру со старого
/// сообщения и прячет тексты вида «что-то пошло не так» в <see cref="InvalidMessage"/>.
///
/// Раньше это были extension-методы на <see cref="ITelegramBotClient"/>; теперь клиент
/// живёт здесь, чтобы экраны и действия не таскали его через конструкторы.
/// </summary>
public class BotMessageSender(ILogger logger, ITelegramBotClient client)
{
    public async Task<Message> SendOrEdit(UserContext userContext, int userId, BotMessage botMessage, bool forceNewMessage = false)
    {
        // FilterPositions в BotMessage больше не ходит в базу сам — ListPositions
        // должен быть загружен здесь, вместе с остальными спутниками пользователя
        var userModel = userContext.Users
            .Include(u => u.ListPositions)
            .First(u => u.UserId == userId);

        Message message;

        try
        {
            await botMessage.Init(userContext, userModel);
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

                // Do not send invalid message as notification, it's useless and annoying
                if (botMessage is not INotificationMessage)
                {
                    var invalidMessage = new InvalidMessage(logger);
                    var sentMessage = await SendOrEdit(userContext, userId, invalidMessage, forceNewMessage: true);
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

            var shouldSendNewMessage =
                userModel.LastBotMessageId < 0
                || botMessage.ForceNewMessage
                || forceNewMessage
                // Между экраном и этим вводом приходило уведомление: экран остаётся выше него,
                // поэтому отвечаем новым сообщением, чтобы экран всегда был снизу
                || userModel.NotifiedSinceLastMessage
                || userModel.ReceivedBroadcasts.Any(b => b.MessageId == userModel.LastBotMessageId);

            if (shouldSendNewMessage)
            {
                if (photoFileId is null)
                {
                    message = await client.SendMessage(chatId: userModel.TelegramId, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: LinkPreviewOptions.Disabled);
                }
                else
                {
                    var photo = InputFile.FromFileId(photoFileId);
                    message = await client.SendPhoto(chatId: userModel.TelegramId, photo: photo, caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
                }

                logger.Information("Sent '{text}' to [{id}] with inline keyboard, messageId [{messageId}]", text, userModel.UserId, message.MessageId);
            }
            else
            {
                if (photoFileId == null)
                {
                    try
                    {
                        message = await client.EditMessageText(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: LinkPreviewOptions.Disabled);
                    }
                    catch (Exception) // Message contains media
                    {
                        logger.Information("Failed to edit text of message [{messageId}], looks like it contains media", userModel.LastBotMessageId);

                        message = await client.SendMessage(chatId: userModel.TelegramId, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: LinkPreviewOptions.Disabled);

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

                logger.Information("Edited [{messageId}] for [{userId}] to '{text}'", userModel.LastBotMessageId, userModel.UserId, text);
            }

            // Уведомление не владеет интерфейсом: экран остаётся тем же, его клавиатуру
            // не гасим, а LastBotMessageId продолжает указывать на сообщение-интерфейс.
            if (botMessage is not INotificationMessage)
            {
                if (userModel.LastBotMessageId is int lastBotMessageId && lastBotMessageId != message.MessageId)
                    await ClearKeyboard(userModel.TelegramId, lastBotMessageId);

                userModel.LastBotMessageId = message.MessageId;
                userModel.NotifiedSinceLastMessage = false;
            }
        }
        catch (Exception e)
        {
            logger.Fatal(e.ToString());
            return null;
        }

        return message;
    }

    public async Task ClearKeyboard(long chatId, long telegramMessageId, bool suppressLog = false)
    {
        var empty = new InlineKeyboardMarkup() { InlineKeyboard = Enumerable.Empty<IEnumerable<InlineKeyboardButton>>() };

        try
        {
            await client.EditMessageReplyMarkup(chatId: chatId, messageId: (int)telegramMessageId, replyMarkup: empty);
            if (!suppressLog)
                logger.Information("Cleared keyboard on message [{messageId}], chatId [{chatId}]", telegramMessageId, chatId);
        }
        catch (Exception ex)
        {
            if (!suppressLog)
                logger.Error("Failed to clear keyboard on message [{messageId}], chatId [{chatId}], error: {ex}", telegramMessageId, chatId, ex.ToString());
        }
    }

    /// <summary>Короткий ответ на нажатие кнопки: тост по умолчанию, алерт — по запросу.</summary>
    public Task AnswerCallbackQuery(string callbackQueryId, string text, bool showAlert = false) =>
        client.AnswerCallbackQuery(callbackQueryId, text, showAlert: showAlert);
}
