using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages.Profile;

[ChildMessage(typeof(EditProfileMessage))]
public class SetProfileNotesMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.Profile).First(u => u.UserId == userId);

        if (string.IsNullOrWhiteSpace(user.Profile.Notes))
        {
            Text.Verbatim("Введите текст заметок ").Italic("(например: \"размер одежды M, увлекаюсь warhammer, не ем сладкое\")").Verbatim(":");
        }
        else
        {
            Text.Bold("Текущие заметки:").LineBreak();

            if (user.Profile.Notes.Length >= 256)
            {
                Text.Monospace(user.Profile.Notes);
            }
            else
            {
                Text.ExpandableQuote(user.Profile.Notes);
                Keyboard.AddCopyTextButton("Скопировать заметки", user.Profile.Notes);
            }

            Text.LineBreak().LineBreak().Verbatim("Введите новое значение:");
        }

        Keyboard
           .NewRow()
           .AddButton<EditProfileQuery>("Отмена");

        user.BotState = BotState.ListerForProfileNotes;

        return Task.CompletedTask;
    }
}


