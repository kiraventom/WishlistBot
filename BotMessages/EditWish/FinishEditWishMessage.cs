using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo)]
public class FinishEditWishMessage(ILogger logger) : BotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SetWishNameQuery>("Добавить ещё виш")
           .NewRow();

        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .ThenInclude(d => d.Original)
            .ThenInclude(u => u.Links)
            .Include(u => u.Wishes)
            .ThenInclude(u => u.Links)
            .First(u => u.UserId == userId);

        var draft = user.CurrentWish;

        if (draft.Original is not null)
            await EditWish(userContext, user, draft);
        else
            await AddWish(userContext, user, draft);

        if (parameters.Pop(QueryParameterType.ReturnToFullList))
            Keyboard.NewRow().AddButton<FullListQuery>("Назад к списку");
        else
            Keyboard.NewRow().AddButton<CompactListQuery>("Назад к моим вишам");

        user.CurrentWish = null;
    }

    protected override async Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SetWishNameQuery>("Добавить ещё виш")
           .NewRow();

        if (parameters.Pop(QueryParameterType.SetWishTo, out var wishId))
            await Legacy_EditWish(user, wishId);
        else
            await Legacy_AddWish(user);

        if (parameters.Pop(QueryParameterType.ReturnToFullList))
            Keyboard.NewRow().AddButton<FullListQuery>("Назад к списку");
        else
            Keyboard.NewRow().AddButton<CompactListQuery>("Назад к моим вишам");

        user.CurrentWish = null;
    }

    private async Task AddWish(UserContext userContext, UserModel user, WishDraftModel draft)
    {
        if (draft is null)
        {
            Logger.Error("[{uId}]: New wish is null", user.UserId);
            throw new NotSupportedException("Current wish is null");
        }

        var newWish = WishModel.FromDraft(draft);

        user.Wishes.Add(newWish);
        userContext.SaveChanges();

        Text.Italic("Виш добавлен!");

        var wishIndex = user.GetSortedWishes().IndexOf(newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<EditWishQuery>("Изменить виш",
                new QueryParameter(QueryParameterType.SetWishTo, newWish.WishId),
                new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                QueryParameter.ReturnToFullList);

        var newWishNotification = new NotificationModel()
        {
            SourceId = user.UserId,
            SubjectId = newWish.WishId,
            Type = NotificationMessageType.NewWish
        };

        await NotificationService.Instance.SendToSubscribers(newWishNotification, userContext);
    }

    private async Task Legacy_AddWish(BotUser user)
    {
        var newWish = user.CurrentWish;

        if (newWish is null)
        {
            Logger.Error("[{uId}]: New wish is null", user.SenderId);
            throw new NotSupportedException("Current wish is null");
        }

        user.Wishes.Add(newWish);
        Text.Italic("Виш добавлен!");

        var wishIndex = user.Wishes.IndexOf(newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<EditWishQuery>("Изменить виш",
                new QueryParameter(QueryParameterType.SetWishTo, newWish.Id),
                new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                QueryParameter.ReturnToFullList);

        var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
        await NotificationService.Instance.Legacy_SendToSubscribers(newWishNotification, user);
    }

    private async Task EditWish(UserContext userContext, UserModel user, WishDraftModel draft)
    {
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

            Text.Italic("Ваши изменения сохранены, но произошла ошибка. Сообщите разработчику об этом");
            return;
        }

        Text.Italic("Виш изменён!");

        var wishPropertyType = WishPropertyType.None;

        if (draft.Original.Name != draft.Name)
            wishPropertyType |= WishPropertyType.Name;

        if (draft.Original.Description != draft.Description)
            wishPropertyType |= WishPropertyType.Description;

        if (!draft.Original.Links.SequenceEqual(draft.Links))
            wishPropertyType |= WishPropertyType.Links;

        if (draft.Original.FileId != draft.FileId)
            wishPropertyType |= WishPropertyType.Media;

        user.Wishes.Remove(draft.Original);
        user.Wishes.Add(editedWish);
        userContext.SaveChanges();

        var wishIndex = user.GetSortedWishes().IndexOf(editedWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<EditWishQuery>("Изменить виш",
                new QueryParameter(QueryParameterType.SetWishTo, editedWish.WishId),
                new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                QueryParameter.ReturnToFullList);

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
    }

    private async Task Legacy_EditWish(BotUser user, long wishId)
    {
        var editedWish = user.CurrentWish;
        var wishBeforeEditing = user.Wishes.FirstOrDefault(w => w.Id == wishId);
        if (wishBeforeEditing is null)
        {
            Logger.Error("Can't find wish {id} to remove after editing", wishId);

            if (editedWish is null)
            {
                Logger.Error("[{uId}]: Edited wish is null", user.SenderId);
                throw new NotSupportedException("Edited wish is null");
            }

            user.Wishes.Add(editedWish);
            Text.Italic("Ваши изменения сохранены, но произошла ошибка. Сообщите разработчику об этом");
            return;
        }

        Text.Italic("Виш изменён!");

        var wishIndex = user.Wishes.IndexOf(wishBeforeEditing);
        user.Wishes.Remove(wishBeforeEditing);
        user.Wishes.Insert(wishIndex, editedWish);

        var wishPropertyType = WishPropertyType.None;

        if (wishBeforeEditing.Name != editedWish.Name)
            wishPropertyType |= WishPropertyType.Name;

        if (wishBeforeEditing.Description != editedWish.Description)
            wishPropertyType |= WishPropertyType.Description;

        if (!wishBeforeEditing.Links.SequenceEqual(editedWish.Links))
            wishPropertyType |= WishPropertyType.Links;

        if (wishBeforeEditing.FileId != editedWish.FileId)
            wishPropertyType |= WishPropertyType.Media;

        if (wishPropertyType != WishPropertyType.None)
        {
            var editWishNotification = new EditWishNotificationMessage(Logger, user, editedWish, wishPropertyType);
            await NotificationService.Instance.Legacy_SendToSubscribers(editWishNotification, user);
        }
    }
}
