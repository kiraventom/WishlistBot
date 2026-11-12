using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedListPositions(ListPosition.Friend)]
[AllowedTypes(QueryParameterType.ReturnToFriends, QueryParameterType.SetListPageTo, QueryParameterType.ChangeWishSortOrder, QueryParameterType.ChangeWishSortProperty, QueryParameterType.WishFilterToggleUnclaimed)]
[MessageFor(typeof(CompactListQuery))]
public class CompactListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        const string plusEmoji = "\u2795";

        var users = userContext.Users
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
                if (parameters.Peek(QueryParameterType.ReturnToFriends))
                    Keyboard.AddButton<MyFriendsQuery>("Назад");
                else
                    Keyboard.AddButton<FriendQuery>("Назад");
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

        var countText = totalCount == sortedCount
            ? $"{sortedCount} {PluralUtils.Wishes(sortedCount)}"
            : $"{sortedCount} из {totalCount} {PluralUtils.WishesGenitive}";

        if (isReadOnly)
            Text.Heading(text => text.Verbatim("Виши ").InlineMention(targetUser).Verbatim($" ({countText})"));
        else
            Text.Heading($"Ваши виши ({countText})");

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

        // Сортировка и её кнопки не нужны, когда показывать нечего сортировать
        var showSorting = sortedCount > 1;

        if (showSorting)
            Text.Italic($"Сортировка: {sortPropertyText}").LineBreak();

        if (isReadOnly)
        {
            const string unclaimedWishesEmoji = "\U0001F513";
            const string allWishesEmoji = "\U0001F539";

            var toggleUnclaimedButton = wishViewSettings.OnlyUnclaimed
                ? $"{unclaimedWishesEmoji} Без брони"
                : $"{allWishesEmoji} Все виши";

            if (wishViewSettings.OnlyUnclaimed)
                Text.Italic("Фильтр: Только виши без брони").LineBreak();

            Keyboard.NewRow();

            if (wishViewSettings.OnlyUnclaimed || sortedWishes.Any(sw => sw.HasVisibleClaim))
                Keyboard.AddButton<CompactListQuery>(toggleUnclaimedButton, QueryParameter.WishFilterToggleUnclaimed);

            if (showSorting)
            {
                Keyboard
                    .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty)
                    .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);
            }

            // Пришли из списка друзей — туда и возвращаемся, иначе — в профиль друга
            var page = parameters.Peek(QueryParameterType.ReturnToFriends)
                ? TextListMessageUtils.AddListControls<CompactListQuery, MyFriendsQuery>(Keyboard, parameters, sortedCount, wishViewSettings, buttonCaption: "Страница")
                : TextListMessageUtils.AddListControls<CompactListQuery, FriendQuery>(Keyboard, parameters, sortedCount, wishViewSettings, buttonCaption: "Страница");

            AddWishTable(userContext, sortedWishes, targetUser.SubscribeId, isReadOnly, page);
        }
        else
        {
            if (showSorting)
            {
                Keyboard
                    .NewRow()
                    .AddButton<CompactListQuery>(sortPropertyButton, QueryParameter.ChangeWishSortProperty)
                    .AddButton<CompactListQuery>(sortOrderButton, QueryParameter.ChangeWishSortOrder);
            }

            Keyboard.NewRow()
                .AddButton<SetWishNameQuery>($"{plusEmoji} Добавить виш", QueryParameter.ForceNewWish);

            sender.CurrentWish = null;

            var page = TextListMessageUtils.AddListControls<CompactListQuery, MainMenuQuery>(Keyboard, parameters, sortedCount, wishViewSettings, buttonCaption: "Страница");

            AddWishTable(userContext, sortedWishes, targetUser.SubscribeId, isReadOnly, page);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Список вишей таблицей: номер, название ссылкой, цена и метки (описание, фото, ссылки).
    /// Кнопки листания добавляются до таблицы — порядок кнопок от этого не зависит.
    /// </summary>
    private void AddWishTable(
        UserContext userContext,
        IReadOnlyList<WishModel> sortedWishes,
        string subscribeId,
        bool isReadOnly,
        ListPage page)
    {
        Text.Table(table =>
        {
            table
                .Header("#", "Виш", "Цена", "Ссылка", "\U0001f512")
                .Align(TableAlign.Right, TableAlign.Left, TableAlign.Right, TableAlign.Center, TableAlign.Center);

            for (var i = 0; i < page.Count; ++i)
            {
                var wish = sortedWishes[page.FirstIndex + i];
                var number = page.FirstIndex + i + 1;
                var isBooked = wish.HasVisibleClaim && isReadOnly;

                table.Row(
                    cell => cell.Verbatim(number.ToString()),
                    cell => AddWishName(cell, wish, subscribeId, isBooked),
                    cell => cell.Verbatim(MessageTextUtils.PriceToShortString(wish.PriceRange)),
                    cell => AddWishLink(cell, wish),
                    cell => AddWishClaim(cell, userContext, wish, isBooked));
            }
        });
    }

    /// <summary>
    /// Название ссылкой. Метки описания и фото идут сразу за названием, в той же ячейке;
    /// забронированный виш зачёркнут — так видно, что его уже подарят.
    /// </summary>
    private static void AddWishName(MessageText text, WishModel wish, string subscribeId, bool isBooked)
    {
        // Постоянный виш помечаем звёздочкой перед названием
        if (wish.IsPermanent)
            text.Verbatim("\u2b50 ");

        text.InlineUrl(name =>
        {
            if (isBooked)
                name.Strikethrough(wish.Name);
            else
                name.Verbatim(wish.Name);
        }, wish.BuildLink(subscribeId));

        if (!string.IsNullOrEmpty(wish.Description))
            text.Verbatim(" \U0001f4ac"); // speech bubble

        if (wish.MediaKey is not null)
            text.Verbatim(" \U0001f5bc\ufe0f"); // picture
    }

    /// <summary>Колонка «Ссылка»: глобус ведёт на первую ссылку виша, иначе ячейка пустая.</summary>
    private static void AddWishLink(MessageText text, WishModel wish)
    {
        if (wish.Links.Any())
            text.InlineUrl("\U0001f310", wish.Links.First().Url);
    }

    /// <summary>Колонка брони: замочек ссылается на того, кто забронировал.</summary>
    private static void AddWishClaim(MessageText text, UserContext userContext, WishModel wish, bool isBooked)
    {
        if (!isBooked)
            return;

        userContext.Entry(wish).Reference(w => w.Claimer).Load();
        text.InlineMention("\U0001f512", wish.Claimer.TelegramId);
    }
}
