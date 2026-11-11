using Telegram.Bot.Types;

namespace WishlistBot.Extra;

/// <summary>
/// Какие входящие сообщения бот игнорирует целиком — до того, как завести пользователя
/// и отдать сообщение слушателям.
/// </summary>
public static class UpdateFilter
{
    /// <summary>
    /// Сообщение не от живого собеседника: от бота или сервисное про топик.
    ///
    /// Когда бот создаёт топик в приватном чате, Telegram присылает сервисное сообщение
    /// «топик создан», и отправителем в нём указан сам бот. Без этого фильтра бот
    /// регистрировал себя как нового пользователя с именем «Вишлист БОТ».
    /// </summary>
    public static bool IsIgnorable(Message message)
    {
        if (message.From?.IsBot == true)
            return true;

        return message.ForumTopicCreated is not null
               || message.ForumTopicEdited is not null
               || message.ForumTopicClosed is not null
               || message.ForumTopicReopened is not null;
    }
}
