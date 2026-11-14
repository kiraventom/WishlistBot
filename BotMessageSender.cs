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
using WishlistBot.Text;

namespace WishlistBot;

/// <summary>
/// Отправка экранов бота: решает, править ли текущее сообщение пользователя или слать
/// новое, хранит <see cref="UserModel.LastBotMessageId"/>, убирает клавиатуру со старого
/// сообщения и прячет тексты вида «что-то пошло не так» в <see cref="InvalidMessage"/>.
///
/// Экраны уходят rich-сообщениями (<c>sendRichMessage</c> / <c>editMessageText</c> с полем
/// <c>rich_message</c>): текст собирается в Rich Markdown, а картинка — медиа-блок внутри
/// того же сообщения. Из-за этого пропали отдельные ветки для фото (sendPhoto,
/// editMessageMedia, editMessageCaption) и лимит подписи в 1024 символа.
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
            var text = botMessage.Text;
            var keyboardMarkup = botMessage.Keyboard.ToInlineKeyboardMarkup();

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
                message = await SendRichMessage(userModel.TelegramId, text, keyboardMarkup);
                logger.Information("Sent '{text}' to [{id}] with inline keyboard, messageId [{messageId}]", text, userModel.UserId, message.MessageId);
            }
            else
            {
                try
                {
                    message = await EditRichMessage(userModel.TelegramId, userModel.LastBotMessageId.Value, text, keyboardMarkup);
                }
                catch (Exception e) when (IsMessageNotModified(e))
                {
                    // «Что это?» и подобные запросы перерисовывают тот же экран: править
                    // нечего, и пересылать новое сообщение тоже не надо — Telegram уже
                    // показывает ровно то, что мы собрали
                    logger.Debug("Screen of [{userId}] did not change, nothing to edit", userModel.UserId);
                    return null;
                }
                catch (Exception e)
                {
                    // Сообщение могло быть отправлено старой версией бота (обычный текст или
                    // фото с подписью) и не принять rich-разметку — тогда шлём новое и убираем старое
                    logger.Information("Failed to edit message [{messageId}] into a rich message ({error}), sending a new one", userModel.LastBotMessageId, e.Message);

                    message = await SendRichMessage(userModel.TelegramId, text, keyboardMarkup);

                    try
                    {
                        await client.DeleteMessage(chatId: userModel.TelegramId, messageId: userModel.LastBotMessageId.Value);
                    }
                    catch (Exception)
                    {
                        logger.Information("Failed to delete message [{messageId}], looks like it was sent more than 48 hours ago", userModel.LastBotMessageId);
                    }
                }

                logger.Information("Edited [{messageId}] for [{userId}] to '{text}'", userModel.LastBotMessageId, userModel.UserId, text);
            }

            await RememberFileIds(text, message);

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

    /// <summary>
    /// Telegram отвечает этим на правку, которая ничего не меняет. Это не ошибка:
    /// экран уже такой, какой нужен.
    /// </summary>
    private static bool IsMessageNotModified(Exception exception) =>
        exception is ApiRequestException { ErrorCode: 400 } &&
        exception.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase);

    private Task<Message> SendRichMessage(long chatId, MessageText text, InlineKeyboardMarkup keyboardMarkup) =>
        SendWithMediaRetry(chatId, text, keyboardMarkup, (richMessage, markup) =>
            client.SendRichMessage(chatId: chatId, richMessage: richMessage, replyMarkup: markup));

    private Task<Message> EditRichMessage(long chatId, int messageId, MessageText text, InlineKeyboardMarkup keyboardMarkup) =>
        SendWithMediaRetry(chatId, text, keyboardMarkup, (richMessage, markup) =>
            client.EditMessageText(chatId: chatId, messageId: messageId, text: null, richMessage: richMessage, replyMarkup: markup));

    /// <summary>
    /// Собирает rich-сообщение и отправляет его. Если Telegram отверг закэшированный file_id
    /// (файл удалён, сменился токен бота), картинки заливаются блобом заново — ровно один раз.
    /// </summary>
    private async Task<Message> SendWithMediaRetry(
        long chatId,
        MessageText text,
        InlineKeyboardMarkup keyboardMarkup,
        Func<InputRichMessage, InlineKeyboardMarkup, Task<Message>> send)
    {
        var prepared = await PrepareMedia(text);
        var markdown = text.ToString();

        try
        {
            return await send(BuildRichMessage(markdown, prepared), keyboardMarkup);
        }
        // Миниатюры вишей — кастомные эмодзи. Если Telegram отверг id стикера (набор
        // удалили, кончился Premium, бот переехал на другой токен), он валит ВСЁ сообщение:
        // RICH_MESSAGE_EMOJI_DOC_INVALID. Пользователь должен увидеть экран любой ценой,
        // поэтому перерисовываем его без эмодзи — с маркерами 🖼️ — и гасим миниатюры
        // на сутки (проверено: rich-probe/dead_emoji_set_probe.py).
        catch (ApiRequestException e) when (IsEmojiRejected(e))
        {
            logger.Warning("Telegram rejected custom emoji ids in the screen for chat [{chatId}] ({error}), resending it without thumbnails",
                chatId, e.Message);

            EmojiPackService.Instance.Disable();

            return await send(BuildRichMessage(MessageTextUtils.StripEmojiLinks(markdown), prepared), keyboardMarkup);
        }
        // «message is not modified» — это не отказ file_id, а «экран не изменился».
        // Перезаливка блоба в этом случае заменяла картинку в сообщении (у легаси-вишей
        // блоб — превью, так что картинка становилась крошечной — см. «Что это?»)
        catch (ApiRequestException e) when (e.ErrorCode == 400
                && !IsMessageNotModified(e)
                && prepared.Any(media => media.Photo.FileId is not null && MediaStorage.Instance.HasData(media.Photo.Key)))
        {
            logger.Warning("Telegram rejected cached file ids for [{keys}] ({error}), re-uploading stored pictures",
                string.Join(", ", prepared.Select(media => media.Photo.Key)), e.Message);

            var reuploaded = prepared
                .Select(media => new PreparedMedia(media.Key, PreparedPhoto.FromData(media.Key, MediaStorage.Instance.GetData(media.Key))))
                .ToList();

            return await send(BuildRichMessage(markdown, reuploaded), keyboardMarkup);
        }
    }

    /// <summary>Telegram отверг id кастомного эмодзи — падает всё сообщение, а не одна иконка.</summary>
    private static bool IsEmojiRejected(Exception exception) =>
        exception is ApiRequestException { ErrorCode: 400 } &&
        exception.Message.Contains("RICH_MESSAGE_EMOJI_DOC_INVALID", StringComparison.OrdinalIgnoreCase);

    private static InputRichMessage BuildRichMessage(string markdown, IReadOnlyList<PreparedMedia> media)
    {
        // Картинки, для которых нет ни file_id, ни блоба, выкидываем вместе со ссылкой:
        // иначе Telegram ответит RICH_MESSAGE_PHOTO_INVALID и экран не уйдёт вообще
        foreach (var missing in media.Where(m => m.Photo is null))
            markdown = markdown.Replace($"![]({MessageTextUtils.GetMediaLink(missing.Key)})", string.Empty);

        var attachments = media
            .Where(m => m.Photo is not null)
            .Select(m => new InputRichMessageMedia
            {
                Id = MessageTextUtils.GetMediaId(m.Key),
                Media = new InputMediaPhoto(ToInputFile(m.Photo))
            })
            .ToList();

        return new InputRichMessage
        {
            Markdown = markdown,
            Media = attachments.Count == 0 ? null : attachments
        };
    }

    private static async Task<List<PreparedMedia>> PrepareMedia(MessageText text)
    {
        var prepared = new List<PreparedMedia>();

        foreach (var key in text.MediaKeys.Distinct())
            prepared.Add(new PreparedMedia(key, await MediaStorage.Instance.PrepareAsync(key)));

        return prepared;
    }

    /// <summary>
    /// Telegram отдаёт file_id картинок только в самом сообщении, поэтому кэш обновляем
    /// по ответу: фото-блоки идут в том же порядке, что и медиа в тексте.
    /// </summary>
    private async Task RememberFileIds(MessageText text, Message message)
    {
        var photoBlocks = message.RichMessage?.Blocks?
            .Where(block => block.Type == RichBlockType.Photo)
            .OfType<RichBlockPhoto>()
            .ToList();

        if (photoBlocks is null || photoBlocks.Count == 0)
            return;

        var keys = text.MediaKeys.Distinct().ToList();

        for (var i = 0; i < Math.Min(keys.Count, photoBlocks.Count); ++i)
        {
            var largest = photoBlocks[i].Photo?.Largest();

            if (largest?.FileId is null)
                continue;

            await MediaStorage.Instance.RememberFileIdAsync(keys[i], largest.FileId);

            // Легаси-картинки импортированы превью (старый бот хранил file_id самого
            // маленького размера): если Telegram показал размер крупнее нашего блоба,
            // полный блоб докачивается после коммита апдейта
            MediaStorage.Instance.RequestUpgradeIfPreview(keys[i], largest.FileId, largest.Width, largest.Height);
        }
    }

    private static InputFile ToInputFile(PreparedPhoto photo) =>
        photo.FileId is not null
            ? InputFile.FromFileId(photo.FileId)
            : InputFile.FromStream(new MemoryStream(photo.Data), "photo.jpg");

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

    private sealed record PreparedMedia(string Key, PreparedPhoto Photo);
}
