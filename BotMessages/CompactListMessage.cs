using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReadOnly, QueryParameterType.ReturnToSubscriber)]
public class CompactListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users
            .Include(u => u.Wishes)
            .ThenInclude(w => w.Links)
            .AsNoTracking();

        var (sender, targetUser) = GetSenderAndTarget(users, userId, parameters);
        var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);

        const string plusEmoji = "\u2795";

        if (!isReadOnly)
            Keyboard.AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);

        if (targetUser.Wishes.Count != 0)
            Keyboard.AddButton<FullListQuery>();

        Keyboard.NewRow();

        if (isReadOnly)
        {
            if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
                Keyboard.AddButton<SubscriberQuery>("К подписчику");
            else
                Keyboard.AddButton<SubscriptionQuery>("К подписке");
        }
        else
        {
            Keyboard.AddButton<MainMenuQuery>("В главное меню");
        }

        if (isReadOnly)
            Text.Bold("Краткий список вишей ")
               .InlineMention(targetUser)
               .Bold(":");
        else
            Text.Bold("Краткий список ваших вишей:");

        var sortedWishes = targetUser.GetSortedWishes();
        for (var i = 0; i < sortedWishes.Count; ++i)
        {
            var wish = sortedWishes[i];
            Text.LineBreak().Bold($"{i + 1}. ");

            // If user isn't looking at its own wishes
            if (wish.ClaimerId != null && sender != targetUser)
            {
                Text.Bold("[БРОНЬ] ").Strikethrough(wish.Name);
            }
            else
            {
                Text.Verbatim(wish.Name);
            }

            if (wish.PriceRange != Price.NotSet)
            {
                Text.Verbatim(" [").Bold(MessageTextUtils.PriceToShortString(wish.PriceRange)).Verbatim("] ");
            }

            if (!string.IsNullOrEmpty(wish.Description))
                Text.Verbatim(" \U0001f4ac"); // speech bubble

            if (wish.FileId is not null)
                Text.Verbatim(" \U0001f5bc\ufe0f"); // picture

            if (wish.Links.Any())
            {
                var firstLink = wish.Links.First().Url;
                Text.InlineUrl(" \U0001f310", firstLink); // globe
            }
        }

        return Task.CompletedTask;
    }
}
