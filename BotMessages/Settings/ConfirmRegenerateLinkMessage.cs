using Serilog;
using WishlistBot.Model;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Settings;

public class ConfirmRegenerateLinkMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string exclamations = "\u203c\ufe0f";

        Text
           .Italic("Перегенерировать ссылку на ваш вишлист?")
           .LineBreak()
           .LineBreak().Verbatim(exclamations).Bold("Обратите внимание, что после этого действия старая ссылка перестанет работать навсегда!");

        Keyboard
           .NewRow().AddButton<SettingsQuery>("Подтвердить", QueryParameter.RegenerateLink)
           .NewRow().AddButton<SettingsQuery>("Отмена");

        return Task.CompletedTask;
    }
}
