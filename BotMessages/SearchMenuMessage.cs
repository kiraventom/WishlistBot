using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Queries;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

public class SearchMenuMessage(ILogger logger, string searchQuery = null) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.First(u => u.UserId == userId);


        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            Text
                .Bold("Поиск по публичным профилям")
                .LineBreak()
                .LineBreak()
                .Italic("Введите тэг человека, чей вишлист вы хотите найти:");
        }
        else
        {
            if (searchQuery.StartsWith('@'))
                searchQuery = searchQuery[1..];

            var result = userContext.Users
                .Include(u => u.Profile)
                .FirstOrDefault(u => u.Tag == searchQuery);

            if (result is null)
            {
                Text.Bold("Ничего не найдено!")
                    .LineBreak()
                    .LineBreak()
                    .Verbatim("Пользователь не зарегистрирован в боте");
            }
            else if (!result.Profile.IsPublic)
            {
                Text.Bold("Пользователь найден, но он закрыл вишлист!")
                    .LineBreak()
                    .LineBreak()
                    .Verbatim($"У пользователя ").Bold(result.FirstName).Verbatim(" закрытый профиль.")
                    .LineBreak()
                    .Verbatim("Попросите его открыть профиль или прислать вам ссылку на его вишлист.");
            }
            else
            {
                Text.Bold("Пользователь найден!")
                    .LineBreak()
                    .InlineUrl(result.FirstName, result.GetSubscribeLink());
            }
        }

        Keyboard.AddButton<MainMenuQuery>("Назад");

        user.BotState = BotState.ListenForSearchQuery;
        return Task.CompletedTask;
    }
}

