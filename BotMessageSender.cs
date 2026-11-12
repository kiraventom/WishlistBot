using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Media;
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
            var photo = botMessage.PhotoMediaKey is null
                ? null
                : await MediaStorage.Instance.PrepareAsync(botMessage.PhotoMediaKey);

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
                if (photo is null)
                {
                    message = await client.SendMessage(chatId: userModel.TelegramId, text: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2, linkPreviewOptions: LinkPreviewOptions.Disabled);
                }
                else
                {
                    message = await SendPhotoAsync(userModel.TelegramId, photo, text, keyboardMarkup);
                }

                logger.Information("Sent '{text}' to [{id}] with inline keyboard, messageId [{messageId}]", text, userModel.UserId, message.MessageId);
            }
            else
            {
                if (photo is null)
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
                    message = await ReplacePhotoAsync(userModel.TelegramId, userModel.LastBotMessageId.Value, photo, text, keyboardMarkup);
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

    private static InputFile ToInputFile(PreparedPhoto photo) =>
        photo.FileId is not null
            ? InputFile.FromFileId(photo.FileId)
            : InputFile.FromStream(new MemoryStream(photo.Data), "photo.jpg");

    /// <summary>
    /// Можно ли перезалить картинку: отправляли по file_id, Telegram ответил 400 и у нас
    /// есть сохранённый блоб. Причину отказа не разбираем по тексту — если блоб есть,
    /// дешевле залить его и получить новый file_id, чем гадать.
    /// </summary>
    private static bool CanReupload(ApiRequestException exception, PreparedPhoto photo) =>
        photo.FileId is not null
        && exception.ErrorCode == 400
        && MediaStorage.Instance.HasData(photo.Key);

    private async Task<Message> SendPhotoAsync(long chatId, PreparedPhoto photo, string text, InlineKeyboardMarkup keyboardMarkup)
    {
        try
        {
            return await client.SendPhoto(chatId: chatId, photo: ToInputFile(photo), caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
        }
        catch (ApiRequestException e) when (CanReupload(e, photo))
        {
            logger.Warning("Telegram rejected cached file id for media '{key}' ({error}), re-uploading the stored picture",
                photo.Key, e.Message);

            var reuploaded = PreparedPhoto.FromData(photo.Key, MediaStorage.Instance.GetData(photo.Key));
            var message = await client.SendPhoto(chatId: chatId, photo: ToInputFile(reuploaded), caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);

            var newFileId = message.Photo.Largest()?.FileId;
            await MediaStorage.Instance.RememberFileIdAsync(photo.Key, newFileId);

            logger.Warning("Media '{key}' is re-uploaded and cached a new file id '{fileId}'", photo.Key, newFileId);
            return message;
        }
    }

    private async Task<Message> ReplacePhotoAsync(long chatId, int messageId, PreparedPhoto photo, string text, InlineKeyboardMarkup keyboardMarkup)
    {
        Message edited;

        try
        {
            edited = await client.EditMessageMedia(chatId: chatId, messageId: messageId, media: new InputMediaPhoto(ToInputFile(photo)), replyMarkup: keyboardMarkup);
        }
        catch (ApiRequestException e) when (CanReupload(e, photo))
        {
            logger.Warning("Telegram rejected cached file id for media '{key}' ({error}), re-uploading the stored picture",
                photo.Key, e.Message);

            var reuploaded = PreparedPhoto.FromData(photo.Key, MediaStorage.Instance.GetData(photo.Key));
            edited = await client.EditMessageMedia(chatId: chatId, messageId: messageId, media: new InputMediaPhoto(ToInputFile(reuploaded)), replyMarkup: keyboardMarkup);

            var newFileId = edited?.Photo.Largest()?.FileId;
            await MediaStorage.Instance.RememberFileIdAsync(photo.Key, newFileId);

            logger.Warning("Media '{key}' is re-uploaded and cached a new file id '{fileId}'", photo.Key, newFileId);
        }

        return await client.EditMessageCaption(chatId: chatId, messageId: messageId, caption: text, replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
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
