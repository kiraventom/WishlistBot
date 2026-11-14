using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Media;
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
    public const int WishesPerPage = TextListMessageUtils.TableRowsPerPage;

    private const string ClockEmoji = "\U0001f551";
    private const string AscendingTriangle = "\U0001F53A";
    private const string DescendingTriangle = "\U0001F53B";
    private const string PlusEmoji = "\u2795";

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
                // Пустой список — это ноль вишей, но предел проверяем одинаково во всех ветках:
                // правило живёт в одном месте
                if (!WishLimits.IsFull(totalCount))
                    Keyboard.AddButton<SetWishNameQuery>($"{PlusEmoji} Добавить виш", QueryParameter.ForceNewWish);

                Keyboard.NewRow().AddButton<MainMenuQuery>("Назад");
            }

            return Task.CompletedTask;
        }

        // Фильтр «Все / Без брони» имеет смысл только когда в списке есть и забронированные,
        // и незабронированные: если все забронированы или ни один — он ничего не меняет
        var bookedCount = targetUser.Wishes.Count(wish => wish.HasVisibleClaim);
        var showFilter = isReadOnly && bookedCount > 0 && bookedCount < totalCount;

        // Недоступный фильтр не должен оставлять список отфильтрованным: иначе выйдет
        // пустой экран без кнопки, чтобы вернуться к полному списку
        if (!showFilter)
            wishViewSettings.OnlyUnclaimed = false;

        var sortedWishes = targetUser.GetSortedWishes(wishViewSettings).ToList();

        if (isReadOnly)
            Text.Heading(text => text.Verbatim("Виши ").InlineMention(targetUser));
        else
            Text.Heading("Ваши виши");

        // «Добавить виш» — над рядом листания, чтобы не искать её под «Назад».
        // На пределе вишей кнопки нет: каждая картинка виша — ещё и стикер в наборе
        // кастомных эмодзи, а у набора лимит 200 (см. WishLimits).
        if (!isReadOnly)
        {
            sender.CurrentWish = null;
            Keyboard.NewRow();

            if (!WishLimits.IsFull(totalCount))
                Keyboard.AddButton<SetWishNameQuery>($"{PlusEmoji} Добавить виш", QueryParameter.ForceNewWish);
        }

        // Куда ведёт «Назад»: свой список — в главное меню, чужой — в профиль друга,
        // а если пришли из списка друзей — обратно в список
        ListPage page;

        if (!isReadOnly)
            page = TextListMessageUtils.AddListControls<CompactListQuery, MainMenuQuery>(
                Keyboard, parameters, sortedWishes.Count, wishViewSettings, buttonCaption: "Страница", WishesPerPage);
        else if (parameters.Peek(QueryParameterType.ReturnToFriends))
            page = TextListMessageUtils.AddListControls<CompactListQuery, MyFriendsQuery>(
                Keyboard, parameters, sortedWishes.Count, wishViewSettings, buttonCaption: "Страница", WishesPerPage);
        else
            page = TextListMessageUtils.AddListControls<CompactListQuery, FriendQuery>(
                Keyboard, parameters, sortedWishes.Count, wishViewSettings, buttonCaption: "Страница", WishesPerPage);

        // Сортировка одного виша ничего не решает — прячем строку вместе с кнопками
        AddWishTable(userContext, sortedWishes, targetUser.SubscribeId, isReadOnly, showFilter,
            showSort: totalCount > 1, wishViewSettings, page);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Таблица вишей: название (со словом БРОНЬ и метками) и цена.
    ///
    /// Сортировка — строкой над таблицей (кнопка свойства и кнопка направления),
    /// фильтр «Все / Без брони» — кнопкой в шапке колонки «Виш». Строка сортировки
    /// скрыта, когда виш всего один, фильтр — когда в списке нет смеси забронированных
    /// и незабронированных: там кнопки ничего не меняют.
    /// </summary>
    private void AddWishTable(
        UserContext userContext,
        IReadOnlyList<WishModel> sortedWishes,
        string subscribeId,
        bool isReadOnly,
        bool showFilter,
        bool showSort,
        WishViewSettingsModel viewSettings,
        ListPage page)
    {
        var wishesOnPage = Enumerable.Range(page.FirstIndex, page.Count)
            .Select(index => sortedWishes[index])
            .ToList();

        var showPrice = wishesOnPage.Any(wish => wish.PriceRange != Price.NotSet);

        var filterData = showFilter
            ? Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.WishFilterToggleUnclaimed)
            : null;

        if (showSort)
            AddSortLine(viewSettings);

        Text.Table(table =>
        {
            var headerCells = new List<Action<MessageText>>
            {
                cell =>
                {
                    cell.Verbatim("Виш ");

                    if (filterData is not null)
                        cell.Button(viewSettings.OnlyUnclaimed ? "Без брони" : "Все", filterData);
                },
            };

            if (showPrice)
            {
                table.Align(TableAlign.Left, TableAlign.Right);
                headerCells.Add(cell => cell.Verbatim("Цена"));
            }
            else
            {
                table.Align(TableAlign.Left);
            }

            table.Header([.. headerCells]);

            if (page.PageIndex > 0)
                AddRemainingRow(table, page.FirstIndex, showPrice);

            // Миниатюры одним запросом на всю страницу: в ячейке рисуется стикер-эмодзи
            var emojiIds = MediaStorage.GetEmojiIds(wishesOnPage.Select(wish => wish.MediaKey));

            foreach (var wish in wishesOnPage)
            {
                var isBooked = wish.HasVisibleClaim && isReadOnly;

                table.Row(cells =>
                {
                    // У виша без фото ключа нет — и словарь про null-ключ спрашивать нельзя
                    var emojiId = wish.MediaKey is null ? null : emojiIds.GetValueOrDefault(wish.MediaKey);

                    cells.Add(cell => AddWishName(cell, userContext, wish, subscribeId, isBooked, emojiId));

                    if (showPrice)
                        cells.Add(cell => cell.Verbatim(MessageTextUtils.PriceToShortString(wish.PriceRange)));
                });
            }

            // Дальше есть ещё виши — строка-многоточие с числом оставшихся
            var remaining = sortedWishes.Count - (page.FirstIndex + page.Count);
            if (remaining > 0)
                AddRemainingRow(table, remaining, showPrice);
        });
    }

    /// <summary>
    /// Строка «Сортировка: [свойство] [направление]» — кнопки переключают режим.
    /// Обе кнопки живут в тексте, поэтому регистрируются через <c>AddTextButton</c>.
    /// </summary>
    private void AddSortLine(WishViewSettingsModel viewSettings)
    {
        var sortByDate = viewSettings.SortProperty == SortProperty.Default;

        var switchSortData = Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.ChangeWishSortProperty);
        var directionData = Keyboard.AddTextButton<CompactListQuery>(parameters: QueryParameter.ChangeWishSortOrder);

        var sortPropertyCaption = sortByDate ? "По дате добавления" : "По цене";

        var sortOrderCaption = (sortByDate, viewSettings.Descending) switch
        {
            (true, false) => "От старых к новым",
            (true, true) => "От новых к старым",
            (false, false) => "От дешёвых к дорогим",
            (false, true) => "От дорогих к дешёвым",
        };

        Text.Verbatim("Сортировка: ");
        Text.Button(sortPropertyCaption, switchSortData);
        Text.Verbatim(" ").Button(sortOrderCaption, directionData);
        Text.BlockBreak();
    }

    /// <summary>
    /// Строка «ещё N вишей…» — обычный текст, а не кнопка: нажатие на неё приходило
    /// битыми callback-данными (HTML-экранирование '&amp;' в tg-button), а листание и так
    /// есть кнопками клавиатуры «Страница N ➡️» / «⬅️ N». Склонение — родительный падеж
    /// («ещё 3 вишей»), как в списке было изначально.
    /// </summary>
    private static void AddRemainingRow(TableBuilder table, int wishesCount, bool showPrice) =>
        TextListMessageUtils.AddRemainingRow(
            table,
            TextListMessageUtils.RemainingCaption(wishesCount, PluralUtils.WishesGenitive),
            showPrice ? 2 : 1);

    /// <summary>
    /// Название ссылкой. У забронированного виша над названием идёт слово БРОНЬ
    /// (ссылка на забронировавшего), а само название зачёркнуто.
    /// </summary>
    private static void AddWishName(MessageText text, UserContext userContext, WishModel wish, string subscribeId, bool isBooked, string emojiId)
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

        // Картинка: миниатюра-эмодзи, а если её ещё нет — маркер 🖼️
        if (wish.MediaKey is not null)
            text.Verbatim(" ").MediaEmoji(emojiId);

        if (wish.Links.Any())
            text.Verbatim(" ").InlineUrl("\U0001f310", wish.Links.First().Url); // globe
    }
}
