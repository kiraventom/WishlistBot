using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedListPositions(ListPosition.Wish, ListPosition.Subscriber, ListPosition.Subscription)]
[AllowedTypes(QueryParameterType.ReturnToSubscriber, QueryParameterType.SetListPageTo, QueryParameterType.ChangeWishSortOrder, QueryParameterType.ChangeWishSortProperty, QueryParameterType.WishFilterToggleUnclaimed)]
public class CompactListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string plusEmoji = "\u2795";

        var users = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.WishPage)
            .Include(u => u.CurrentWish)
            .Include(u => u.Wishes).ThenInclude(w => w.Links)
            .Include(u => u.WishViewSettings);

        var (sender, targetUser) = GetSenderAndTarget(users, userId, parameters);
        var isReadOnly = sender.UserId != targetUser.UserId;

        var wishViewSettings = sender.GetOrCreateViewerTarget<WishViewSettingsModel>(targetUser.UserId);

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

        if (totalCount == 0)
        {
            Text.Bold("Вишлист пуст :(");
            if (isReadOnly)
            {
                if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
                    Keyboard.AddButton<SubscriberQuery>("Назад");
                else
                    Keyboard.AddButton<SubscriptionQuery>("Назад");
            }
            else
            {
                Keyboard
                    .AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish)
                    .NewRow()
                    .AddButton<MainMenuQuery>("Назад");
            }

            return Task.CompletedTask;
        }

        var sortedWishes = targetUser.GetSortedWishes(wishViewSettings).ToList();
        var sortedCount = sortedWishes.Count;

        var countText = totalCount == sortedCount ? $"{sortedCount}" : $"{sortedCount} из {totalCount}";

        if (isReadOnly)
        {
            Text.Bold("Виши ").InlineMention(targetUser);
        }
        else
        {
            Text.Bold($"Ваши виши");
        }

        Text.Bold($" ({countText} вишей)");

        var sortPropertyText = wishViewSettings.SortProperty switch
        {
            SortProperty.Default when wishViewSettings.Descending => "От новых к старым",
            SortProperty.Price when wishViewSettings.Descending => "От дорогих к дешёвым",
            SortProperty.Default => "От старых к новым",
            SortProperty.Price => "От дешёвых к дорогим",
            _ => "ERROR"
        };

        const string ascendingEmoji = "\U0001F53A";
        const string descsendingEmoji = "\U0001F53B";
        const string calendarEmoji = "\U0001F4C5";
        const string dollarEmoji = "\U0001F4B2";

        var sortPropertyButton = wishViewSettings.SortProperty switch
        {
            SortProperty.Default => $"{calendarEmoji} По дате",
            SortProperty.Price => $"{dollarEmoji} По цене",
            _ => "ERROR"
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

            Keyboard.NewRow();

            if (wishViewSettings.OnlyUnclaimed || sortedWishes.Any(sw => sw.ClaimerId != null))
                Keyboard.AddButton<CompactListQuery>(toggleUnclaimedButton, QueryParameter.WishFilterToggleUnclaimed);

            Keyboard
                .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty) 
                .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);

            if (parameters.Peek(QueryParameterType.ReturnToSubscriber))
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriberQuery>(Text, Keyboard, parameters, sortedCount, sender.ListPositions.WishPage, itemIndex =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, targetUser.SubscribeId, itemIndex, isReadOnly);
                }, buttonCaption: "Страница");
            }
            else
            {
                TextListMessageUtils.AddListControls<CompactListQuery, SubscriptionQuery>(Text, Keyboard, parameters, sortedCount, sender.ListPositions.WishPage, itemIndex =>
                {
                    var wish = sortedWishes[itemIndex];
                    AddWishText(userContext, wish, targetUser.SubscribeId, itemIndex, isReadOnly);
                }, buttonCaption: "Страница");
            }
        }
        else
        {
            Text.LineBreak().LineBreak();

            Keyboard
                .NewRow()
                .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty) 
                .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);

            Keyboard.NewRow()
                .AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);

            sender.CurrentWish = null;

            TextListMessageUtils.AddListControls<CompactListQuery, MainMenuQuery>(Text, Keyboard, parameters, sortedCount, sender.ListPositions.WishPage, itemIndex =>
            {
                var wish = sortedWishes[itemIndex];
                AddWishText(userContext, wish, targetUser.SubscribeId, itemIndex, isReadOnly);
            }, buttonCaption: "Страница");
        }

        return Task.CompletedTask;
    }

    private void AddWishText(UserContext userContext, WishModel wish, string subscribeId, int itemIndex, bool isReadonly)
    {
        Text.Bold($"{itemIndex + 1}. ");

        // If user isn't looking at its own wishes
        if (wish.ClaimerId != null && isReadonly)
        {
            userContext.Entry(wish).Reference(w => w.Claimer).Load();
            Text.Bold("[").InlineMention(wish.Claimer, "БРОНЬ").Bold("] ");
        }

        // TODO strikethrough if claimed
        Text.InlineUrl(wish.Name, wish.BuildLink(subscribeId));

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
