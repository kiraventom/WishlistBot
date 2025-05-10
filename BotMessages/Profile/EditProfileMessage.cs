using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;

namespace WishlistBot.BotMessages.Profile;

public class EditProfileMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users.Include(u => u.Profile);
        var sender = users.First(u => u.UserId == userId);

        const string calendar = "\U0001f4c5 ";
        const string writingHand = "\u270d\ufe0f ";

        Text.Verbatim("Ваш профиль:").LineBreak();

        // Birthday
        Text.LineBreak().Verbatim(calendar);

        if (sender.Profile.Birthday is null)
        {
            Text.Italic("Дата рождения не установлена");
            Keyboard.AddButton<SetProfileBirthdayQuery>("Установить дату рождения");
        }
        else
        {
            Text.Bold("Дата рождения: ").Monospace(sender.Profile.Birthday.Value.ToString("dd.MM.yyyy"));
            Keyboard.AddButton<SetProfileBirthdayQuery>("Изменить дату рождения");
        }


        Keyboard.NewRow();

        // Notes
        Text.LineBreak().Verbatim(writingHand);

        if (string.IsNullOrWhiteSpace(sender.Profile.Notes))
        {
            Text.Italic("Заметки пусты. Здесь можно указать размер одежды, общие интересы, пожелания или просто написать какую-то чушь");
            Keyboard.AddButton<SetProfileNotesQuery>("Добавить заметки");
        }
        else
        {
            Text.Bold("Заметки: ").LineBreak().ExpandableQuote(sender.Profile.Notes);
            Keyboard.AddButton<SetProfileNotesQuery>("Изменить заметки");
        }

        Keyboard.NewRow().AddButton<MainMenuQuery>("Назад");

        return Task.CompletedTask;
    }
}
