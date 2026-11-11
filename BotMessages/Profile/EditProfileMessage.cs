using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
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
        const string downArrow = "\u2b07\ufe0f";
        const string link = "\U0001f517";

        // Invite link button: копирует в буфер только саму ссылку
        Keyboard.AddCopyTextButton($"{link} Скопировать ссылку", sender.GetSubscribeLink());

        Keyboard.NewRow();

        // Birthday
        Text.Verbatim(calendar);

        if (sender.Profile.Birthday is null)
        {
            Text.Italic("Дата рождения не установлена");
            Keyboard.AddButton<SetProfileBirthdayQuery>("\U0001f4c5 Установить дату рождения");
        }
        else
        {
            Text.Bold("Дата рождения: ").Monospace(sender.Profile.Birthday.Value.ToString("dd.MM.yyyy"));
            Keyboard.AddButton<SetProfileBirthdayQuery>("\U0001f4c5 Изменить дату рождения");
        }

        Keyboard.NewRow();

        // Notes
        Text.LineBreak().Verbatim(writingHand);

        if (string.IsNullOrWhiteSpace(sender.Profile.Notes))
        {
            Text.Italic("Заметки пусты. Здесь можно указать размер одежды, общие интересы, пожелания или просто написать какую-то чушь");
            Keyboard.AddButton<SetProfileNotesQuery>("\u270f\ufe0f Добавить заметки");
        }
        else
        {
            Text.Bold("Заметки: ").LineBreak().ExpandableQuote(sender.Profile.Notes);
            Keyboard.AddButton<SetProfileNotesQuery>("\u270f\ufe0f Изменить заметки");
        }

        Keyboard.NewRow().AddButton<MainMenuQuery>("Назад");

        
        // Invite link text
        Text.LineBreak().LineBreak().Verbatim($"{downArrow} Ссылка на ваш вишлист {downArrow}");

        return Task.CompletedTask;
    }
}
