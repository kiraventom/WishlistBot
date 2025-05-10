using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages.Profile;

[ChildMessage(typeof(EditProfileMessage))]
public class SetProfileBirthdayMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.Profile).First(u => u.UserId == userId);

        Keyboard
           .NewRow()
           .AddButton<EditProfileQuery>("Отмена");

        if (user.Profile.Birthday is null)
        {
            Text.Verbatim("Укажите дату рождения в формате ").Monospace("ДД.ММ.ГГГГ")
                .LineBreak()
                .Italic("Пример: 08.04.1998");
        }
        else
        {
            Text
               .Bold("Указанная дата рождения: ").Monospace(user.Profile.Birthday.Value.ToString("dd.MM.yyyy"))
               .LineBreak()
               .LineBreak().Verbatim("Введите новое значение:");
        }

        user.BotState = BotState.ListenForBirthday;

        return Task.CompletedTask;
    }
}


