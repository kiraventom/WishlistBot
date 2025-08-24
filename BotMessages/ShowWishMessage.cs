using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries.EditWish;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages;

// TODO REFACTOR
[AllowedTypes(QueryParameterType.WishId, QueryParameterType.ClaimWish, QueryParameterType.ReturnToMyClaims, QueryParameterType.CleanDraft, QueryParameterType.SaveDraft)]
[ChildMessage(typeof(CompactListMessage))]
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
            if (parameters.Pop(QueryParameterType.ClaimWish))
            {
                // Claim unclaimed wish
                if (wish.ClaimerId is null)
                {
                    wish.ClaimerId = userId;
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

            if (wish.ClaimerId is not null)
            {
                var claimer = userContext.Users.FirstOrDefault(u => u.UserId == wish.ClaimerId);
                if (claimer is not null)
                {
                    if (claimer.UserId == userId)
                    {
                        Text.ItalicBold("Этот виш забронирован вами").LineBreak().LineBreak();

                        if (parameters.Peek(QueryParameterType.ReturnToMyClaims))
                        {
                            Keyboard
                                .AddButton<MyClaimsQuery>("Снять бронь", QueryParameter.ClaimWish, new QueryParameter(QueryParameterType.WishId, wish.WishId))
                                .NewRow();
                        }
                        else
                        {
                            Keyboard.AddButton<ShowWishQuery>("Снять бронь", new QueryParameter(QueryParameterType.WishId, wishId), QueryParameter.ClaimWish)
                                .NewRow();
                        }
                    }
                    else
                    {
                        Text.ItalicBold("\u203c\ufe0f Этот виш забронирован ").InlineMention(claimer).ItalicBold("! \u203c\ufe0f").LineBreak().LineBreak();
                    }
                }
                else
                {
                    Logger.Error("Wish [{wishId}]: Claimer [{claimerId}] not found in database. Cleaning ClaimerId", wish.WishId, wish.ClaimerId);
                    wish.ClaimerId = null;
                }
            }

            // Checking ClaimerId in separate if because it can be reset when claimer was not found in database
            if (wish.ClaimerId is null)
            {
                Keyboard.AddButton<ShowWishQuery>("Забронировать", new QueryParameter(QueryParameterType.WishId, wishId), QueryParameter.ClaimWish)
                    .NewRow();
            }
        }
        else
        {
            if (parameters.Pop(QueryParameterType.CleanDraft))
            {
                userContext.Entry(sender).Reference(u => u.CurrentWish).Load();
                
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

        Text.Bold("Название: ").Monospace(name);

        if (description is not null)
            Text.LineBreak().Bold("Описание: ").LineBreak().ExpandableQuote(description);

        if (links.Any())
        {
            Text.LineBreak().Bold("Ссылки: ");
            for (var i = 0; i < links.Count; ++i)
            {
                var link = links[i].Url;
                Text.InlineUrl(link);
                if (i < links.Count - 1)
                    Text.Verbatim(", ");
            }
        }

        if (priceRange != Price.NotSet)
        {
            var priceRangeString = MessageTextUtils.PriceToString(priceRange);
            Text.LineBreak().Bold("Цена: ").Monospace(priceRangeString);
        }

        PhotoFileId = wish.FileId;

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

            Keyboard.AddButton<MyClaimsQuery>("Назад");

            if (index < totalCount - 1)
                Keyboard.AddButton<ShowWishQuery>($"{nextIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.WishId, claimedWishes[nextIndex].WishId));
        }
        else
        {
            userContext.Entry(wish).Reference(w => w.Owner).Load();
            userContext.Entry(wish.Owner).Collection(c => c.Wishes).Load();

            var wishes = wish.Owner.GetSortedWishes();

            var totalCount = wishes.Count;
            var index = wishes.IndexOf(wish);
            var prevIndex = index - 1;
            var nextIndex = index + 1;

            if (index > 0)
                Keyboard.AddButton<ShowWishQuery>($"\u2b05\ufe0f {prevIndex + 1}", new QueryParameter(QueryParameterType.WishId, wishes[prevIndex].WishId));

            Keyboard.AddButton<CompactListQuery>("Назад");

            if (index < totalCount - 1)
                Keyboard.AddButton<ShowWishQuery>($"{nextIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.WishId, wishes[nextIndex].WishId));
        }
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

        await NotificationService.Instance.SendToSubscribers(newWishNotification, userContext);

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

        if (draft.Original.FileId != draft.FileId)
            wishPropertyType |= WishPropertyType.Media;

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

            await NotificationService.Instance.SendToSubscribers(editWishNotification, userContext);
        }

        return editedWish;
    }
}
