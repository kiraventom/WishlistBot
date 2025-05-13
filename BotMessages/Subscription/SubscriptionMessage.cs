using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(MySubscriptionsMessage))]
public class SubscriptionMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.Profile)
            .First(u => u.UserId == targetId);

        Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");

        if (target.Wishes.Count != 0)
        {
            Keyboard
               .NewRow()
               .AddButton<CompactListQuery>("Открыть вишлист");
        }

        Keyboard
           .NewRow()
           .AddButton<MySubscriptionsQuery>("К моим подпискам");

        Text.Bold("Подписка на ")
           .InlineMention(target)
           .Bold(":");

        const string calendar = "\U0001f4c5 ";
        const string writingHand = "\u270d\ufe0f ";
        const string gift = "\U0001f381 ";

        if (target.Profile.Birthday != null)
        {
            var birthday = target.Profile.Birthday.Value;
            Text
                .LineBreak()
                .Verbatim(calendar)
                .Bold("Дата рождения: ")
                .Monospace(birthday.ToString("dd.MM.yyyy"));

            var today = DateTime.Now;
            var nextBirthday = new DateTime(today.Year, birthday.Month, birthday.Day);

            if (nextBirthday < today)
                nextBirthday = nextBirthday.AddYears(1);

            var daysBeforeBirthday = (int)Math.Round(nextBirthday.Subtract(today).TotalDays);
            var nextAge = nextBirthday.Year - birthday.Year;

            var ageEnding = GetYearEndingDeclination(nextAge);
            var days = GetDaysDeclinated(daysBeforeBirthday);
            Text.Italic($" ({nextAge}-{ageEnding} ДР через {daysBeforeBirthday} {days})");
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

        Text
           .LineBreak()
           .Verbatim(gift)
           .Bold("Вишей в вишлисте: ")
           .Monospace(target.Wishes.Count.ToString());

        return Task.CompletedTask;
    }

    private static string GetYearEndingDeclination(int year)
    {
        if (year == 0)
            return "ой";

        var lastTwoDigits = year % 100;

        if (lastTwoDigits is >= 10 and <= 20)
            return "ый";

        var lastDigit = year % 10;
        return lastDigit switch
        {
            1 or 4 or 5 or 9 or 0 => "ый",
            2 or 6 or 7 or 8 => "ой",
            3 => "ий",
            _ => throw new NotSupportedException("This won't happen lol")
        };
    }

    private static string GetDaysDeclinated(int days)
    {
        var lastTwoDigits = days % 100;

        if (lastTwoDigits is >= 10 and <= 20)
            return "дней";

        var lastDigit = days % 10;
        return lastDigit switch
        {
            1 => "день",
            2 or 3 or 4 => "дня",
            _ => "дней"
        };
    }
}
