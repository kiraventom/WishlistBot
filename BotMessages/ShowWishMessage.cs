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

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.WishId, QueryParameterType.ClaimWish, QueryParameterType.ReturnToMyClaims, QueryParameterType.SaveDraft)]
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

        if (links.Any())
        {
            Text.BlockBreak().Bold("Ссылки:");

            Text.Bullets(links.Count, (line, index) => line.InlineUrl(links[index].Url));
        }

        if (priceRange != Price.NotSet)
        {
            var priceRangeString = MessageTextUtils.PriceToString(priceRange);
            Text.BlockBreak().Bold("Цена: ").Monospace(priceRangeString);
        }

        if (wish.IsPermanent)
            Text.BlockBreak().Verbatim($"\u2b50 Постоянный виш: {target.FirstName} всегда ему обрадуется!");

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
            userContext.Entry(wish.Claimer).Collection(c => c.ClaimedWishes).Load();

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

        userContext.Entry(wish).Reference(w => w.Claimer).Load();

        Text.Verbatim("Этот виш забронирован ").InlineMention(wish.Claimer);

        if (wish.ClaimerId != userId)
            return;

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

    private async Task<WishModel> EditWish(UserContext userContext, UserModel user, WishDraftModel draft)
    {
        userContext.Entry(draft).Reference(d => d.Original).Load();
        userContext.Entry(draft.Original).Collection(d => d.Links).Load();

        var editedWish = WishModel.FromDraft(draft);

        if (draft.Original is null)
        {
            Logger.Error("Draft {draftId} is not connected to original", draft.WishDraftId);

            if (draft is null)
            {
                Logger.Error("[{uId}]: Edited wish is null", user.UserId);
                throw new NotSupportedException("Edited wish is null");
            }

            userContext.Wishes.Add(editedWish);
            userContext.SaveChanges();
            return editedWish;
        }

        var wishPropertyType = WishPropertyType.None;

        if (draft.Original.Name != draft.Name)
            wishPropertyType |= WishPropertyType.Name;

        if (draft.Original.Description != draft.Description)
            wishPropertyType |= WishPropertyType.Description;

        if (!draft.Original.Links.SequenceEqual(draft.Links))
            wishPropertyType |= WishPropertyType.Links;

        if (draft.Original.MediaKey != draft.MediaKey)
            wishPropertyType |= WishPropertyType.Media;

        // О смене типа виша сообщаем отдельно: только когда виш стал постоянным
        var becamePermanent = draft.IsPermanent && !draft.Original.IsPermanent;

        DeleteWish(userContext, user, draft.Original);
        user.Wishes.Add(editedWish);
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
}
