using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReturnToSubscriber, QueryParameterType.SetListPageTo)]
public class CompactListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users
            .Include(u => u.CurrentWish)
            .Include(u => u.Wishes)
            .ThenInclude(w => w.Links)
            .AsNoTracking();

        var (sender, targetUser) = GetSenderAndTarget(users, userId, parameters);
        var isReadOnly = sender.UserId != targetUser.UserId;

        const string plusEmoji = "\u2795";

        if (!isReadOnly)
            Keyboard.AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);

        Keyboard.NewRow();

        var sortedWishes = targetUser.GetSortedWishes();
        var totalCount = sortedWishes.Count;

        if (isReadOnly)
            Text.Bold("Виши ")
               .InlineMention(targetUser)
               .Bold($" ({totalCount}):");
        else
            Text.Bold($"Ваши виши ({totalCount}):");

        Text.LineBreak();

        if (isReadOnly)
        {
            if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriberQuery>(Text, Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, itemIndex, pageIndex, isReadOnly);
                });
            }
            else
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriptionQuery>(Text, Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, itemIndex, pageIndex, isReadOnly);
                });
            }
        }
        else
        {
            sender.CurrentWish = null;
                TextListMessageUtils.AddListControls<CompactListQuery, MainMenuQuery>(Text, Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, itemIndex, pageIndex, isReadOnly);
                });
        }

        return Task.CompletedTask;
    }

    private void AddWishText(UserContext userContext, WishModel wish, int itemIndex, int pageIndex, bool isReadonly)
    {
        Text.Bold($"{itemIndex + 1}. ");

        // If user isn't looking at its own wishes
        if (wish.ClaimerId != null && isReadonly)
        {
            userContext.Entry(wish).Reference(w => w.Claimer).Load();
            Text.Bold("[").InlineMention("БРОНЬ", wish.Claimer.Tag).Bold("] ");
        }

        // TODO strikethrough if claimed
        Text.InlineUrl(wish.Name, $"t.me/{Config.Instance.Username}?start=action=showwish_setuserto={wish.OwnerId}_setwishto={wish.WishId}_setlistpageto={pageIndex}");

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
}
