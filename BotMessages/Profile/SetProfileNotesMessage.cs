using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Profile;

[ChildMessage(typeof(EditProfileMessage))]
[MessageFor(typeof(SetProfileNotesQuery))]
public class SetProfileNotesMessage(ILogger logger) : BotMessage(logger)
{
    private const string CopyCaption = "Скопировать";

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.Profile).First(u => u.UserId == userId);

        if (string.IsNullOrWhiteSpace(user.Profile.Notes))
        {
            Text.Verbatim("Введите текст заметок ").Italic("(например: \"размер одежды M, увлекаюсь warhammer, не ем сладкое\")").Verbatim(":");
        }
        else
        {
            Text.Bold("Текущие заметки:");

            // Копирование — кнопкой в тексте: кнопки в клавиатуре забирают место
            // у «Отмены» и расходятся с остальными окнами правки
            if (user.Profile.Notes.Length < MessageText.MaxCopyTextLength)
                Text.Verbatim(" ").CopyButton(CopyCaption, user.Profile.Notes);

            if (user.Profile.Notes.Length >= MessageText.MaxCopyTextLength)
            {
                Text.BlockBreak().Monospace(user.Profile.Notes);
            }
            else
            {
                Text.ExpandableQuote(user.Profile.Notes);
            }

            Text.BlockBreak().Verbatim("Введите новое значение:");
        }

        Keyboard
           .NewRow()
           .AddButton<EditProfileQuery>("Отмена");

        user.BotState = BotState.ListerForProfileNotes;

        return Task.CompletedTask;
    }
}


