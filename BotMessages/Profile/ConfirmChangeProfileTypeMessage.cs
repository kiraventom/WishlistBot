using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages.Profile;

public class ConfirmChangeProfileTypeMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string locked = "\U0001f512";
        const string unlocked = "\U0001f513";

        var users = userContext.Users.Include(u => u.Profile);
        var sender = users.First(u => u.UserId == userId);

        string buttonCaption;

        if (sender.Profile.IsPublic)
        {
            buttonCaption = $"{locked} Закрыть профиль";
            Text
                .Verbatim("Сейчас у вас ").Bold("открытый").Verbatim(" профиль.")
                .LineBreak().LineBreak()
                .Verbatim("\u00b7 Подписчики ").Bold("могут").Verbatim(" копировать ссылку на ваш вишлист")
                .LineBreak()
                .Verbatim("\u00b7 Вас ").Bold("можно").Verbatim(" найти через поиск")
                .LineBreak().LineBreak()
                .Verbatim("После закрытия профиля эти функции станут ").Bold("недоступны").Verbatim(".")
                .LineBreak()
                .Italic("Закрыть профиль?");
        }
        else
        {
            buttonCaption = $"{unlocked} Открыть профиль";
            Text
                .Verbatim("Сейчас у вас ").Bold("закрытый").Verbatim(" профиль.")
                .LineBreak().LineBreak()
                .Verbatim("\u00b7 Подписчики ").Bold("не могут").Verbatim(" копировать ссылку на ваш вишлист")
                .LineBreak()
                .Verbatim("\u00b7 Вас ").Bold("нельзя").Verbatim(" найти через поиск")
                .LineBreak().LineBreak()
                .Verbatim("После открытия профиля эти функции станут ").Bold("доступны").Verbatim(".")
                .LineBreak()
                .Italic("Открыть профиль?");
        }

        Keyboard.AddButton<EditProfileQuery>(buttonCaption, QueryParameter.ChangeProfileType)
            .NewRow()
            .AddButton<EditProfileQuery>("Отмена");

        return Task.CompletedTask;
    }
}

