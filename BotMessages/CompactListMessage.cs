using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReturnToSubscriber, QueryParameterType.SetListPageTo, QueryParameterType.ChangeWishSortOrder, QueryParameterType.ChangeWishSortProperty, QueryParameterType.WishFilterToggleUnclaimed)]
public class CompactListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users
            .Include(u => u.CurrentWish)
            .Include(u => u.Wishes).ThenInclude(w => w.Links)
            .Include(u => u.WishViewSettings);

        var (sender, targetUser) = GetSenderAndTarget(users, userId, parameters);
        var isReadOnly = sender.UserId != targetUser.UserId;

        var wishViewSettings = sender.GetOrCreateWishViewSettings(targetUser.UserId);

        if (parameters.Pop(QueryParameterType.ChangeWishSortOrder))
            wishViewSettings.Descending = !wishViewSettings.Descending;

        if (parameters.Pop(QueryParameterType.ChangeWishSortProperty))
        {
            var newSortProperty = wishViewSettings.SortProperty + 1;
            if (Enum.IsDefined(newSortProperty))
                wishViewSettings.SortProperty = newSortProperty;
            else
                wishViewSettings.SortProperty = SortProperty.Default;
        }

        if (parameters.Pop(QueryParameterType.WishFilterToggleUnclaimed))
            wishViewSettings.OnlyUnclaimed = !wishViewSettings.OnlyUnclaimed;

        var totalCount = targetUser.Wishes.Count;
        var sortedWishes = targetUser.GetSortedWishes(wishViewSettings).ToList();
        var sortedCount = sortedWishes.Count;

        var countText = totalCount == sortedCount ? $"{sortedCount}" : $"{sortedCount} / {totalCount}";

        if (isReadOnly)
            Text.Bold("Виши ")
               .InlineMention(targetUser)
               .Bold($" [{countText}]:");
        else
            Text.Bold($"Ваши виши [{countText}]:");

        var sortPropertyText = wishViewSettings.SortProperty switch
        {
            SortProperty.Default when wishViewSettings.Descending => "От новых к старым",
            SortProperty.Price when wishViewSettings.Descending => "От дорогих к дешёвым",
            SortProperty.Default => "От старых к новым",
            SortProperty.Price => "От дешёвых к дорогим",
        };

        const string ascendingEmoji = "\U0001F53A";
        const string descsendingEmoji = "\U0001F53B";
        const string calendarEmoji = "\U0001F4C5";
        const string dollarEmoji = "\U0001F4B2";

        var sortPropertyButton = wishViewSettings.SortProperty switch
        {
            SortProperty.Default => $"{calendarEmoji} По дате",
            SortProperty.Price => $"{dollarEmoji} По цене",
        };

        var sortOrderButton = wishViewSettings.Descending switch
        {
            false => $"{ascendingEmoji}",
            true => $"{descsendingEmoji}",
        };

        Text.LineBreak()
            .Italic("Сортировка: ").Verbatim(sortPropertyText);

        if (isReadOnly)
        {
            const string unclaimedWishesEmoji = "\U0001F513";
            const string allWishesEmoji = "\U0001F539";

            var toggleUnclaimedButton = wishViewSettings.OnlyUnclaimed 
                ? $"{unclaimedWishesEmoji} Без брони"
                : $"{allWishesEmoji} Все виши";

            if (wishViewSettings.OnlyUnclaimed)
            {
                Text.LineBreak()
                    .Italic("Фильтр: ").Verbatim("Только виши без брони");
            }

            Text.LineBreak().LineBreak();

            Keyboard
                .NewRow()
                .AddButton<CompactListQuery>(toggleUnclaimedButton, QueryParameter.WishFilterToggleUnclaimed)
                .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty) 
                .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);

            if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriberQuery>(Text, Keyboard, parameters, sortedCount, (itemIndex, pageIndex) =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, itemIndex, pageIndex, isReadOnly);
                });
            }
            else
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriptionQuery>(Text, Keyboard, parameters, sortedCount, (itemIndex, pageIndex) =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, itemIndex, pageIndex, isReadOnly);
                });
            }
        }
        else
        {
            Text.LineBreak().LineBreak();

            Keyboard
                .NewRow()
                .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty) 
                .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);

            const string plusEmoji = "\u2795";

            Keyboard.NewRow()
                .AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);

            sender.CurrentWish = null;
                TextListMessageUtils.AddListControls<CompactListQuery, MainMenuQuery>(Text, Keyboard, parameters, sortedCount, (itemIndex, pageIndex) =>
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
            Text.Bold("[").InlineMention(wish.Claimer, "БРОНЬ").Bold("] ");
        }

        // TODO strikethrough if claimed
        Text.InlineUrl(wish.Name, $"t.me/{Config.Instance.Username}?start=action=showwish_userid={wish.OwnerId}_wishid={wish.WishId}_setlistpageto={pageIndex}");

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
