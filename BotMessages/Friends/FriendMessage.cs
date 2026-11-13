using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Text;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Friends;

[ChildMessage(typeof(MyFriendsMessage))]
[MessageFor(typeof(FriendQuery))]
public class FriendMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users
            .Include(u => u.Friends)
            .Include(u => u.Profile)
            .First(u => u.UserId == userId);

        // Профиль — не список: страница, пришедшая из списка друзей, дальше не идёт,
        // иначе она подменит запомненную страницу вишлиста
        parameters.Pop(QueryParameterType.SetListPageTo);

        parameters.Peek(QueryParameterType.UserId, out var targetId);

        var target = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.Profile)
            .FirstOrDefault(u => u.UserId == targetId);

        // Профиль мог не прийти вместе с параметром (старая кнопка, устаревшая ссылка):
        // лучше показать понятный экран, чем упасть на Sequence contains no elements
        if (target is null)
        {
            Text.Bold("Профиль не найден");
            Text.BlockBreak().Italic("Возможно, пользователь удалил аккаунт или ссылка устарела");

            Keyboard.AddButton<MainMenuQuery>("В главное меню");
            return Task.CompletedTask;
        }

        var isSelf = sender.UserId == target.UserId;
        var isFriend = sender.IsFriendOf(target.UserId);

        const string gift = "\U0001f381 ";

        if (target.Wishes.Count != 0)
        {
            // Страницу не передаём: вишлист открывается на той, где зритель его оставил
            Keyboard
               .AddButton<CompactListQuery>($"Открыть вишлист ({gift.Trim()} {target.Wishes.Count})")
               .NewRow();
        }

        if (!isSelf)
        {
            if (isFriend)
                Keyboard.AddButton<ConfirmRemoveFriendQuery>("Удалить из друзей");
            else
                Keyboard.AddButton<AddFriendQuery>();

            // «К друзьям» — отдельной строкой: две кнопки в ряд не помещались
            Keyboard.NewRow();
        }

        const string link = "\U0001f517";

        Keyboard.AddButton<MyFriendsQuery>("К друзьям");

        // Имя — заголовком экрана: в MarkdownV2 заголовков не было, имя было просто жирным
        if (isSelf)
            Text.Heading("Это вы");
        else
            Text.Heading(text => text.InlineMention(target));

        const string calendar = "\U0001f4c5 ";
        const string writingHand = "\u270d\ufe0f ";

        // Плашка показывается всегда: иначе у профиля без заметок и ДР получается
        // слишком узкое сообщение и кнопки не помещаются в один ряд
        Text.Verbatim(calendar).Bold("Дата рождения: ");

        if (target.Profile.Birthday is not { } birthday)
        {
            Text.Verbatim("не указана");
        }
        else
        {
            Text.Date(birthday, "d", birthday.ToString("dd.MM.yyyy"));

            var today = DateOnly.FromDateTime(DateTime.Now);
            var nextBirthday = BirthdayUtils.GetNextBirthday(birthday, today);
            var daysBeforeBirthday = BirthdayUtils.GetDaysUntil(nextBirthday, today);
            var nextAge = BirthdayUtils.GetAge(birthday, nextBirthday);

            var relativeText = daysBeforeBirthday == 0
                ? "сегодня"
                : $"через {daysBeforeBirthday} {PluralUtils.Days(daysBeforeBirthday)}";

            // Тот же формат, что в списке друзей и дайджесте: «32 года через 113 дней»
            Text.Italic(line => line
                .Verbatim($" ({nextAge} {PluralUtils.Years(nextAge)} ")
                .RelativeDate(nextBirthday, relativeText)
                .Verbatim(")"));
        }

        if (target.Profile.Notes != null)
        {
            Text.BlockBreak().Verbatim(writingHand).Bold("Заметки:").BlockBreak();
            Text.ExpandableQuote(target.Profile.Notes);
        }

        // Футер: ссылка на вишлист с кнопкой копирования
        Text.Divider();
        Text.Verbatim("Ссылка на вишлист: ");
        Text.CopyButton($"{link} Скопировать", target.GetSubscribeLink());

        // Если виши есть — их количество показано на кнопке «Открыть вишлист»
        if (target.Wishes.Count == 0)
        {
            Text.BlockBreak()
               .Verbatim(gift)
               .Bold("Вишей в вишлисте: ")
               .Monospace(target.Wishes.Count.ToString());
        }

        return Task.CompletedTask;
    }
}
