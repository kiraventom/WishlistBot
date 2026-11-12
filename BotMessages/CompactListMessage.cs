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
    /// <summary>Виши в таблице крупнее, чем строки остальных списков: 10 на страницу.</summary>
    public const int WishesPerPage = 10;

    private const string ClockEmoji = "\U0001f551";
    private const string AscendingTriangle = "\U0001F53A";
    private const string DescendingTriangle = "\U0001F53B";
    private const string PlusEmoji = "\u2795";
    private const string Dot = "…";

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
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
                    .AddButton<SetWishNameQuery>($"{PlusEmoji} Добавить виш", QueryParameter.ForceNewWish)
                    .NewRow()
                    .AddButton<MainMenuQuery>("Назад");
            }

            return Task.CompletedTask;
        }

        var sortedWishes = targetUser.GetSortedWishes(wishViewSettings).ToList();

        if (isReadOnly)
            Text.Heading(text => text.Verbatim("Виши ").InlineMention(targetUser));
        else
            Text.Heading("Ваши виши");

        // «Добавить виш» — над рядом листания, чтобы не искать её под «Назад»
        if (!isReadOnly)
        {
            sender.CurrentWish = null;
            Keyboard
                .NewRow()
                .AddButton<SetWishNameQuery>($"{PlusEmoji} Добавить виш", QueryParameter.ForceNewWish);
        }

        // Пришли из списка друзей — туда и возвращаемся, иначе — в профиль друга
        var page = parameters.Peek(QueryParameterType.ReturnToFriends)
            ? TextListMessageUtils.AddListControls<CompactListQuery, MyFriendsQuery>(
                Keyboard, parameters, sortedWishes.Count, wishViewSettings, buttonCaption: "Страница", WishesPerPage)
            : TextListMessageUtils.AddListControls<CompactListQuery, FriendQuery>(
                Keyboard, parameters, sortedWishes.Count, wishViewSettings, buttonCaption: "Страница", WishesPerPage);

        AddWishTable(userContext, sortedWishes, targetUser.SubscribeId, isReadOnly, wishViewSettings, page);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Таблица вишей: номер, название (со словом БРОНЬ и метками) и цена.
    ///
    /// Сортировка, её направление и фильтр — кнопки внутри шапки таблицы: часы в первой
    /// колонке сортируют по дате, слово «Цена» — по цене, кнопка справа от «Виш» —
    /// фильтр «Все / Без брони». Активная колонка становится обычным текстом, а рядом
    /// появляется треугольник — он же кнопка смены направления.
    /// </summary>
    private void AddWishTable(
        UserContext userContext,
        IReadOnlyList<WishModel> sortedWishes,
        string subscribeId,
        bool isReadOnly,
        WishViewSettingsModel viewSettings,
        ListPage page)
    {
        var wishesOnPage = Enumerable.Range(page.FirstIndex, page.Count)
            .Select(index => sortedWishes[index])
            .ToList();

        var showPrice = wishesOnPage.Any(wish => wish.PriceRange != Price.NotSet);
        var sortByDate = viewSettings.SortProperty == SortProperty.Default;
        var triangle = viewSettings.Descending ? DescendingTriangle : AscendingTriangle;

        var directionData = Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.ChangeWishSortOrder);
        var switchSortData = Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.ChangeWishSortProperty);
        var filterData = isReadOnly
            ? Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.WishFilterToggleUnclaimed)
            : null;

        Text.Table(table =>
        {
            var headerCells = new List<Action<MessageText>>
            {
                cell =>
                {
                    if (sortByDate)
                    {
                        cell.Verbatim($"{ClockEmoji} ");
                        cell.Button(triangle, directionData);
                    }
                    else
                    {
                        cell.Button(ClockEmoji, switchSortData);
                    }
                },
                cell =>
                {
                    cell.Verbatim("Виш ");

                    if (filterData is not null)
                        cell.Button(viewSettings.OnlyUnclaimed ? "Без брони" : "Все", filterData);
                },
            };

            if (showPrice)
            {
                table.Align(TableAlign.Right, TableAlign.Left, TableAlign.Right);

                headerCells.Add(cell =>
                {
                    if (sortByDate)
                        cell.Button("Цена", switchSortData);
                    else
                    {
                        cell.Verbatim("Цена ");
                        cell.Button(triangle, directionData);
                    }
                });
            }

            if (!showPrice)
                table.Align(TableAlign.Right, TableAlign.Left);

            table.Header([.. headerCells]);

            if (page.PageIndex > 0)
                AddJumpRow(table, page.FirstIndex, 0, showPrice);

            for (var i = 0; i < wishesOnPage.Count; ++i)
            {
                var wish = wishesOnPage[i];
                var number = page.FirstIndex + i + 1;
                var isBooked = wish.HasVisibleClaim && isReadOnly;

                table.Row(cells =>
                {
                    cells.Add(cell => cell.Verbatim(number.ToString()));
                    cells.Add(cell => AddWishName(cell, userContext, wish, subscribeId, isBooked));

                    if (showPrice)
                        cells.Add(cell => cell.Verbatim(MessageTextUtils.PriceToShortString(wish.PriceRange)));
                });
            }

            // Дальше есть ещё виши — строка-многоточие с числом оставшихся
            var remaining = sortedWishes.Count - (page.FirstIndex + page.Count);
            if (remaining > 0)
                AddJumpRow(table, remaining, page.PageIndex + 1, showPrice);
        });
    }

    private void AddJumpRow(TableBuilder table, int wishesCount, int targetPage, bool showPrice)
    {
        var data = Keyboard.AddTextButton<CompactListQuery>(
            parameters: new QueryParameter(QueryParameterType.SetListPageTo, targetPage));

        table.Row(cells =>
        {
            cells.Add(cell => cell.Verbatim(Dot));
            cells.Add(cell => cell.Button($"ещё {wishesCount} {PluralUtils.WishesGenitive}", data));

            if (showPrice)
                cells.Add(cell => cell.Verbatim(Dot));
        });
    }

    /// <summary>
    /// Название ссылкой. У забронированного виша над названием идёт слово БРОНЬ
    /// (ссылка на забронировавшего), а само название зачёркнуто.
    /// </summary>
    private static void AddWishName(MessageText text, UserContext userContext, WishModel wish, string subscribeId, bool isBooked)
    {
        if (isBooked)
        {
            userContext.Entry(wish).Reference(w => w.Claimer).Load();
            text.Verbatim("\U0001f512 ").InlineMention("БРОНЬ", wish.Claimer.TelegramId).CellLineBreak();
        }

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

        if (wish.Links.Any())
            text.Verbatim(" ").InlineUrl("\U0001f310", wish.Links.First().Url); // globe
    }
}
