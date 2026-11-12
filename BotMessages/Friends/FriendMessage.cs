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
            .First(u => u.UserId == targetId);

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
        }

        const string link = "\U0001f517";

        Keyboard
           .NewRow()
           .AddCopyTextButton($"{link} Ссылка на вишлист", target.GetSubscribeLink())
           .NewRow()
           .AddButton<MyFriendsQuery>("К друзьям");

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
            Text.Monospace(birthday.ToString("dd.MM.yyyy"));

            var today = DateOnly.FromDateTime(DateTime.Now);
            var nextBirthday = BirthdayUtils.GetNextBirthday(birthday, today);
            var daysBeforeBirthday = BirthdayUtils.GetDaysUntil(nextBirthday, today);
            var nextAge = BirthdayUtils.GetAge(birthday, nextBirthday);

            // Тот же формат, что в списке друзей и дайджесте: «32 года через 113 дней»
            Text.Italic($" ({BirthdayUtils.DescribeUpcoming(nextAge, daysBeforeBirthday)})");
        }

        if (target.Profile.Notes != null)
        {
            Text.BlockBreak().Verbatim(writingHand).Bold("Заметки:").BlockBreak();
            Text.ExpandableQuote(target.Profile.Notes);
        }

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
