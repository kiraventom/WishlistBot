using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Friends;

[AllowedListPositions(ListPosition.Friend)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MyFriendsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.FriendPage)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Profile)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Wishes)
            .First(u => u.UserId == userId);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var friends = BirthdayUtils.SortByUpcomingBirthday(user.Friends.Select(f => f.Friend), today);
        var totalCount = friends.Count;

        if (totalCount == 0)
        {
            const string downArrow = "\u2b07\ufe0f";
            const string link = "\U0001f517";

            Text.Bold("У вас пока нет друзей :(")
                .LineBreak()
                .Italic($"Пригласите друзей, прислав им ссылку на свой вишлист {downArrow}");

            Keyboard.AddCopyTextButton($"{link} Ссылка на вишлист", user.GetSubscribeLink());

            Keyboard.NewRow();
        }
        else
        {
            Text.Bold("Ваши друзья:").LineBreak().LineBreak();
        }

        TextListMessageUtils.AddListControls<MyFriendsQuery, MainMenuQuery>(Text, Keyboard, parameters, totalCount, user.ListPositions.FriendPage, itemIndex =>
        {
            FriendBirthdayLine.Append(Text, itemIndex + 1, friends[itemIndex]);
        }, buttonCaption: "Страница");

        return Task.CompletedTask;
    }
}
