using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Profile;

[ChildMessage(typeof(EditProfileMessage))]
[MessageFor(typeof(SetProfileBirthdayQuery))]
public class SetProfileBirthdayMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.Profile).First(u => u.UserId == userId);

        if (user.Profile.Birthday is not { } birthday)
        {
            Text.Verbatim("Укажите дату рождения в формате ").Monospace("ДД.ММ.ГГГГ")
                .LineBreak()
                .Italic("Пример: 08.04.1998");
        }
        else
        {
            // У скрытого года в дате его нет: «10.03»
            var birthdayString = birthday.ToString();

            Text
               .Bold("Указанная дата рождения: ").Monospace(birthdayString)
               .Verbatim(" ").CopyButton("Скопировать", birthdayString)
               .BlockBreak().Verbatim("Введите новое значение:");
        }

        Keyboard
           .NewRow()
           .AddButton<EditProfileQuery>("Отмена");


        user.BotState = BotState.ListenForBirthday;

        return Task.CompletedTask;
    }
}


