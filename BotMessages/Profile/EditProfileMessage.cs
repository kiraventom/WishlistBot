using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.Profile;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Profile;

[AllowedTypes(QueryParameterType.ShowHelp, QueryParameterType.ClearWishProperty)]
[MessageFor(typeof(EditProfileQuery))]
public class EditProfileMessage(ILogger logger) : BotMessage(logger)
{
    private const string EditEmoji = "\u270f\ufe0f";
    private const string DeleteEmoji = "\U0001f5d1\ufe0f";

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users.Include(u => u.Profile);
        var sender = users.First(u => u.UserId == userId);

        // 🗑 рядом с заметками: чистим их и рисуем экран заново
        if (parameters.Pop(QueryParameterType.ClearWishProperty))
            sender.Profile.Notes = null;

        if (parameters.Pop(QueryParameterType.ShowHelp))
        {
            AlertText = "Заметки видят друзья в вашем профиле.\n\n" +
                        "Здесь можно указать размер одежды, общие интересы, пожелания " +
                        "или просто написать какую-то чушь";
        }

        // Из reply-кнопок остаётся только «Назад»: всё остальное — в тексте
        Keyboard.AddButton<MainMenuQuery>("Назад");

        Text.Heading("Профиль");

        // Дата рождения: значение через tg-time, справа — правка или «Указать»
        Text.Verbatim("\U0001f4c5 ").Bold("Дата рождения: ");

        if (sender.Profile.Birthday is { } birthday)
        {
            Text.Date(birthday, "d", birthday.ToString("dd.MM.yyyy")).Verbatim(" ");
            Text.Button(EditEmoji, Keyboard.AddTextButton<SetProfileBirthdayQuery>());
        }
        else
        {
            Text.Verbatim("Не указана ");
            Text.Button("Указать", Keyboard.AddTextButton<SetProfileBirthdayQuery>());
        }

        // Заметки: кнопки справа от двоеточия, сам текст — цитатой ниже
        Text.BlockBreak().Verbatim("\u270d\ufe0f ").Bold("Заметки: ");

        if (string.IsNullOrWhiteSpace(sender.Profile.Notes))
        {
            Text.Verbatim("Не указаны ");
            Text.Button("Указать", Keyboard.AddTextButton<SetProfileNotesQuery>());
            Text.Verbatim(" ").Button("Что это?", Keyboard.AddTextButton<EditProfileQuery>(parameters: QueryParameter.ShowHelp));
        }
        else
        {
            Text.Button(EditEmoji, Keyboard.AddTextButton<SetProfileNotesQuery>());
            Text.Verbatim(" ").Button(DeleteEmoji,
                Keyboard.AddTextButton<EditProfileQuery>(parameters: new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Description)));

            Text.BlockBreak();
            Text.ExpandableQuote(sender.Profile.Notes);
        }

        Text.Divider();

        // Ссылка на вишлист — кнопка, которая копирует её в буфер
        Text.CopyButton("\U0001f517 Ссылка на вишлист", sender.GetSubscribeLink());

        return Task.CompletedTask;
    }
}
