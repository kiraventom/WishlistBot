using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Text;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Friends;

[ChildMessage(typeof(MyFriendsMessage))]
public class FriendMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users
            .Include(u => u.Friends)
            .Include(u => u.Profile)
            .First(u => u.UserId == userId);

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
            // Entering the wishlist from the profile always starts from the first page
            Keyboard
               .AddButton<CompactListQuery>($"Открыть вишлист ({gift.Trim()} {target.Wishes.Count})",
                    new QueryParameter(QueryParameterType.SetListPageTo, 0))
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

        // имя отбивается пустой строкой; пустую строку даёт LineBreak первой секции ниже
        if (isSelf)
            Text.Bold("Это вы");
        else
            Text.InlineMention(target);

        Text.LineBreak();

        const string calendar = "\U0001f4c5 ";
        const string writingHand = "\u270d\ufe0f ";

        if (target.Profile.Birthday != null)
        {
            var birthday = target.Profile.Birthday.Value;
            Text
                .LineBreak()
                .Verbatim(calendar)
                .Bold("Дата рождения: ")
                .Monospace(birthday.ToString("dd.MM.yyyy"));

            var today = DateOnly.FromDateTime(DateTime.Now);
            var nextBirthday = BirthdayUtils.GetNextBirthday(birthday, today);
            var daysBeforeBirthday = BirthdayUtils.GetDaysUntil(nextBirthday, today);
            var nextAge = BirthdayUtils.GetAge(birthday, nextBirthday);

            Text.Italic($" ({nextAge}-{PluralUtils.YearsOrdinalEnding(nextAge)} ДР через " +
                        $"{daysBeforeBirthday} {PluralUtils.Days(daysBeforeBirthday)})");
        }

        if (target.Profile.Notes != null)
        {
            var notes = target.Profile.Notes;

            Text
                .LineBreak()
                .Verbatim(writingHand)
                .Bold("Заметки: ")
                .LineBreak();

            Text.Quote(notes);
        }

        // Если виши есть — их количество показано на кнопке «Открыть вишлист»
        if (target.Wishes.Count == 0)
        {
            Text
               .LineBreak()
               .Verbatim(gift)
               .Bold("Вишей в вишлисте: ")
               .Monospace(target.Wishes.Count.ToString());
        }

        return Task.CompletedTask;
    }
}
