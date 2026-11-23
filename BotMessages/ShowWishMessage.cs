using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.EditWish;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Friends;
using WishlistBot.Media;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.WishId, QueryParameterType.ClaimWish, QueryParameterType.ReturnToMyClaims, QueryParameterType.SaveDraft, QueryParameterType.ShowHelp)]
[ChildMessage(typeof(CompactListMessage))]
[MessageFor(typeof(ShowWishQuery))]
public class ShowWishMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users.Include(u => u.Wishes);
        var (sender, target) = GetSenderAndTarget(users, userId, parameters);

        parameters.Pop(QueryParameterType.WishId, out var wishId);

        var wish = target.Wishes.FirstOrDefault(w => w.WishId == wishId);

        if (parameters.Pop(QueryParameterType.SaveDraft))
        {
            userContext.Entry(sender).Reference(u => u.CurrentWish).Load();
            userContext.Entry(sender.CurrentWish).Collection(u => u.Links).Load();

            var draft = sender.CurrentWish;
            WishModel newWish;
            if (draft.Original is not null)
                newWish = await EditWish(userContext, sender, draft);
            else
                newWish = await AddWish(userContext, sender, draft);

            sender.CurrentWish = null;
            wish = newWish;
        }

        if (wish is null)
        {
            Text.Italic("Виш не найден! Вероятно, он был удалён");
            Keyboard
                .AddButton<CompactListQuery>($"К другим вишам {target.FirstName}")
                .NewRow()
                .AddButton<MainMenuQuery>("В главное меню");
            return;
        }

        var readOnly = sender.UserId != target.UserId;

        if (readOnly)
        {
            var claimRequested = parameters.Pop(QueryParameterType.ClaimWish);

            // Постоянный виш бронировать нельзя: кнопок нет, а пришедший запрос игнорируем
            if (!wish.IsPermanent && claimRequested)
            {
                // Claim unclaimed wish
                if (wish.ClaimerId is null)
                {
                    wish.ClaimerId = userId;

                    // If claimer is not a friend of the wish owner, make them friends automatically
                    if (!FriendshipManager.IsFriend(userContext, sender.UserId, target.UserId))
                    {
                        await AddFriendMessage.PerformAddFriend(Logger, userContext, sender, target);
                    }
                }
                // Unclaim wish claimed by sender
                else if (wish.ClaimerId == userId)
                {
                    wish.ClaimerId = null;
                }
                else
                {
                    Logger.Error("ShowWish: parameters contain ClaimWish, but wish.ClaimerId is nor null neither [{senderId}], but [{claimerId}]", userId, wish.ClaimerId);
                }
            }

            if (!wish.IsPermanent && wish.ClaimerId is not null)
            {
                var claimer = userContext.Users.FirstOrDefault(u => u.UserId == wish.ClaimerId);
                if (claimer is null)
                {
                    Logger.Error("Wish [{wishId}]: Claimer [{claimerId}] not found in database. Cleaning ClaimerId", wish.WishId, wish.ClaimerId);
                    wish.ClaimerId = null;
                }
            }
        }
        else
        {
            userContext.Entry(sender).Reference(u => u.CurrentWish).Load();
            if (sender.CurrentWish is not null)
            {
                userContext.WishDrafts.Remove(sender.CurrentWish);
                sender.CurrentWish = null;
            }

            const string pencilEmoji = "\u270f\ufe0f";

            Keyboard
                .AddButton<EditWishQuery>($"{pencilEmoji} Редактировать", new QueryParameter(QueryParameterType.WishId, wish.WishId))
                .NewRow();
        }

        userContext.Entry(wish).Collection(w => w.Links).Load();

        var name = wish.Name;
        var description = wish.Description;
        var links = wish.Links;
        var priceRange = wish.PriceRange;

        // Название — заголовком экрана, а не полем «Название: …»
        Text.Heading(name);

        // Картинка — медиа-блоком в теле сообщения: раньше экран с фото уходил отдельным
        // photo-сообщением, и весь текст был его подписью (лимит 1024 символа)
        Text.Media(wish.MediaKey);

        if (description is not null)
            Text.Quote(description);

        if (links.Count == 1)
        {
            Text.BlockBreak().Bold("Ссылка: ").InlineUrl(links[0].Url);
        }
        else if (links.Count > 1)
        {
            Text.BlockBreak().Bold("Ссылки:");

            Text.Bullets(links.Count, (line, index) => line.InlineUrl(links[index].Url));
        }

        if (priceRange != Price.NotSet)
        {
            var priceRangeString = MessageTextUtils.PriceToString(priceRange);
            Text.BlockBreak().Bold("Цена: ").Monospace(priceRangeString);
        }

        // Параметр вынимаем всегда, даже если кнопки нет: иначе он останется в QueryParams
        // и уедет в кнопки следующего экрана
        var showHelp = parameters.Pop(QueryParameterType.ShowHelp);

        if (wish.IsPermanent)
        {
            Text.BlockBreak().Verbatim("\u2b50 Постоянный виш");

            // «Что это?» — только в чужом окне: у своего виша пояснения нет, свои
            // постоянные виши объясняет окно редактирования (кнопка под «Цена»).
            if (readOnly)
            {
                // WishId передаём явно: его вынули из параметров в начале InitInternal,
                // и без него кнопка открывала «Виш не найден»
                Text.Verbatim(" ").Button("Что это?", Keyboard.AddTextButton<ShowWishQuery>(
                    parameters: [new QueryParameter(QueryParameterType.WishId, wish.WishId), QueryParameter.ShowHelp]),
                    style: "primary");

                // Про чужой виш рассказываем в третьем лице, с именем владельца вишлиста:
                // «которому <имя> всегда обрадуется»
                if (showHelp)
                    AlertText = PermanentWishHelpFor(target.FirstName);
            }
        }

        if (readOnly && !wish.IsPermanent)
            AddClaimLine(userContext, wish, userId, parameters);

        string shareText;
        
        if (readOnly)
        {
        shareText = 
$@"

{target.FirstName} добавил {wish.Name} в свой вишлист!
Открыть в Вишлист БОТ:
{wish.BuildLink(target.SubscribeId)}";
        }
        else
        {
        shareText = 
$@"

Я добавил {wish.Name} в свой вишлист!
Открыть в Вишлист БОТ:
{wish.BuildLink(target.SubscribeId)}";
        }

        Keyboard.AddShareButton("Поделиться", shareText)
            .NewRow();

        // Controls
        if (parameters.Peek(QueryParameterType.ReturnToMyClaims))
        {
            userContext.Entry(wish).Reference(w => w.Claimer).Load();

            // Владелец виша и его профиль нужны для того же порядка, что в списке броней
            // (сортировка по ближайшему ДР) — иначе стрелки ходили бы не по тому списку
            userContext.Entry(wish.Claimer).Collection(c => c.ClaimedWishes)
                .Query().Include(cw => cw.Owner).ThenInclude(owner => owner.Profile).Load();

            var claimedWishes = wish.Claimer.GetSortedClaimedWishes();

            var totalCount = claimedWishes.Count;
            var index = claimedWishes.IndexOf(wish);
            var prevIndex = index - 1;
            var nextIndex = index + 1;

            if (index > 0)
                Keyboard.AddButton<ShowWishQuery>($"\u2b05\ufe0f {prevIndex + 1}", new QueryParameter(QueryParameterType.WishId, claimedWishes[prevIndex].WishId));

            // Return to the page of the claims list that contains this wish
            Keyboard.AddButton<MyClaimsQuery>("Назад",
                new QueryParameter(QueryParameterType.SetListPageTo, TextListMessageUtils.GetPageIndex(index, CompactListMessage.WishesPerPage)));

            if (index < totalCount - 1)
                Keyboard.AddButton<ShowWishQuery>($"{nextIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.WishId, claimedWishes[nextIndex].WishId));
        }
        else
        {
            userContext.Entry(sender).Collection(u => u.WishViewSettings).Load();
            userContext.Entry(wish).Reference(w => w.Owner).Load();
            userContext.Entry(wish.Owner).Collection(c => c.Wishes).Load();

            var wishViewSettings = sender.GetOrCreateViewerTarget<WishViewSettingsModel>(target.UserId);
            var location = wish.Owner.LocateWish(wishViewSettings, wish);
            var wishes = location.Wishes;
            var index = location.Index;

            var totalCount = wishes.Count;
            var prevIndex = index - 1;
            var nextIndex = index + 1;

            if (index > 0)
                Keyboard.AddButton<ShowWishQuery>($"\u2b05\ufe0f {prevIndex + 1}", new QueryParameter(QueryParameterType.WishId, wishes[prevIndex].WishId));

            // Return to the page of the wishlist that contains this wish
            Keyboard.AddButton<CompactListQuery>("Назад",
                new QueryParameter(QueryParameterType.SetListPageTo, TextListMessageUtils.GetPageIndex(index, CompactListMessage.WishesPerPage)));

            if (index < totalCount - 1)
                Keyboard.AddButton<ShowWishQuery>($"{nextIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.WishId, wishes[nextIndex].WishId));
        }
    }

    /// <summary>
    /// Строка о брони внизу экрана: кто забронировал и кнопка действия.
    /// Кнопка живёт в тексте (tg-button), а не в reply_markup.
    /// </summary>
    private void AddClaimLine(UserContext userContext, WishModel wish, int userId, QueryParameterCollection parameters)
    {
        Text.BlockBreak();

        if (wish.ClaimerId is null)
        {
            Text.Verbatim("Этот виш не забронирован ");

            var data = Keyboard.AddTextButton<ShowWishQuery>(
                parameters: [new QueryParameter(QueryParameterType.WishId, wish.WishId), QueryParameter.ClaimWish]);

            Text.Button("Забронировать", data, style: "success");
            return;
        }

        if (wish.ClaimerId == userId)
        {
            Text.Verbatim("Этот виш забронирован вами");
        }
        else
        {
            userContext.Entry(wish).Reference(w => w.Claimer).Load();
            Text.Verbatim("Этот виш забронирован ").InlineMention(wish.Claimer);
            return;
        }

        // Свою бронь снимаем тем же экраном; из «Моих броней» возвращаемся в список
        var claimParameters = new List<QueryParameter>
        {
            new(QueryParameterType.WishId, wish.WishId),
            QueryParameter.ClaimWish
        };

        if (parameters.Peek(QueryParameterType.ReturnToMyClaims))
            claimParameters.Add(QueryParameter.ReturnToMyClaims);

        var claimData = Keyboard.AddTextButton<ShowWishQuery>(parameters: [.. claimParameters]);

        Text.Verbatim(" ").Button("Снять бронь", claimData, style: "danger");
    }

    private async Task<WishModel> AddWish(UserContext userContext, UserModel user, WishDraftModel draft)
    {
        if (draft is null)
        {
            Logger.Error("[{uId}]: New wish is null", user.UserId);
            throw new NotSupportedException("Current wish is null");
        }

        var newWish = WishModel.FromDraft(draft);

        user.Wishes.Add(newWish);
        userContext.SaveChanges();

        var newWishNotification = new NotificationModel()
        {
            SourceId = user.UserId,
            SubjectId = newWish.WishId,
            Type = NotificationMessageType.NewWish
        };

        await NotificationService.Instance.SendToFriends(newWishNotification, userContext);

        return newWish;
    }

    /// <summary>
    /// Применяет черновик к существующему вишу <b>на месте</b>.
    ///
    /// Виш — это его <see cref="WishModel.WishId"/>: по нему ходят все кнопки и ссылки,
    /// которые бот уже выдал (кнопка «Перейти к вишу» в уведомлении, диплинк
    /// <c>a=sw_…</c>, ссылка «Поделиться»). Раньше правка удаляла строку и создавала новую,
    /// и виш получал новый идентификатор — после любого «Готово» (даже без единого
    /// изменения) все ранее отправленные кнопки начинали говорить «Виш не найден!
    /// Вероятно, он был удалён», хотя виш никуда не делся. Поэтому обновляем поля
    /// у той же строки, а бронь и порядок в списке не трогаем вообще.
    /// </summary>
    private async Task<WishModel> EditWish(UserContext userContext, UserModel user, WishDraftModel draft)
    {
        userContext.Entry(draft).Reference(d => d.Original).Load();

        // Черновик без связи с вишем — это не правка, а новый виш (так ведёт себя
        // легаси-черновик, у которого OriginalId пуст)
        if (draft.Original is null)
        {
            Logger.Error("Draft {draftId} is not connected to original, adding it as a new wish", draft.WishDraftId);

            var recoveredWish = WishModel.FromDraft(draft);

            user.Wishes.Add(recoveredWish);
            userContext.SaveChanges();
            return recoveredWish;
        }

        var editedWish = draft.Original;

        userContext.Entry(editedWish).Collection(w => w.Links).Load();

        var wishPropertyType = WishPropertyType.None;

        if (editedWish.Name != draft.Name)
            wishPropertyType |= WishPropertyType.Name;

        if (editedWish.Description != draft.Description)
            wishPropertyType |= WishPropertyType.Description;

        if (!editedWish.Links.SequenceEqual(draft.Links))
            wishPropertyType |= WishPropertyType.Links;

        if (editedWish.MediaKey != draft.MediaKey)
            wishPropertyType |= WishPropertyType.Media;

        // О смене типа виша сообщаем отдельно: только когда виш стал постоянным
        var becamePermanent = draft.IsPermanent && !editedWish.IsPermanent;

        ApplyDraft(userContext, editedWish, draft);

        userContext.SaveChanges();

        if (wishPropertyType != WishPropertyType.None)
        {
            var editWishNotification = new NotificationModel()
            {
                SourceId = user.UserId,
                SubjectId = editedWish.WishId,
                Type = NotificationMessageType.WishEdit,
            };

            editWishNotification.SetExtraInt((int)wishPropertyType);

            // Об изменении виша знает только тот, кто его забронировал
            await NotificationService.Instance.SendToClaimer(editWishNotification, userContext, editedWish.ClaimNotificationRecipientId);
        }

        if (becamePermanent)
        {
            var permanentNotification = new NotificationModel()
            {
                SourceId = user.UserId,
                SubjectId = editedWish.WishId,
                Type = NotificationMessageType.WishPermanent,
            };

            await NotificationService.Instance.SendToFriends(permanentNotification, userContext);
        }

        return editedWish;
    }

    /// <summary>
    /// Переносит поля черновика в виш. Бронь (<see cref="WishModel.ClaimerId"/>) и порядок
    /// в списке (<see cref="WishModel.Order"/>) не трогаем: правка не должна ни снимать бронь,
    /// ни переставлять виш в чужом вишлисте.
    /// </summary>
    private static void ApplyDraft(UserContext userContext, WishModel wish, WishDraftModel draft)
    {
        // Картинка могла смениться. Старый блоб и его стикер-миниатюру освобождаем так же,
        // как при удалении виша: после коммита MediaStorage удалит то, на что никто
        // больше не ссылается (см. UserContext.SaveChanges)
        if (!string.Equals(wish.MediaKey, draft.MediaKey, StringComparison.Ordinal))
            MediaStorage.ReleaseLater([wish.MediaKey]);

        wish.Name = draft.Name;
        wish.Description = draft.Description;
        wish.MediaKey = draft.MediaKey;
        wish.PriceRange = draft.PriceRange;
        wish.IsPermanent = draft.IsPermanent;

        RewriteLinks(userContext, wish, draft);
    }

    /// <summary>
    /// Ссылки виша приводим к набору из черновика. Сравнение — по Url
    /// (<see cref="LinkModel.Equals"/>), у самих строк другой идентификатор.
    /// </summary>
    private static void RewriteLinks(UserContext userContext, WishModel wish, WishDraftModel draft)
    {
        if (wish.Links.SequenceEqual(draft.Links))
            return;

        foreach (var link in wish.Links.ToList())
        {
            wish.Links.Remove(link);
            userContext.Links.Remove(link);
        }

        foreach (var draftLink in draft.Links)
            wish.Links.Add(new LinkModel { Url = draftLink.Url });
    }
}
