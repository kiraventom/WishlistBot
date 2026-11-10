using System.Text;

namespace WishlistBot.Model.User;

/// <summary>
/// Единственное место, где живут магические строки диплинков бота.
///
/// Формат ссылки: <c>t.me/&lt;bot&gt;?start=a=&lt;действие&gt;_&lt;ключ&gt;=&lt;значение&gt;_...</c>
/// Например <c>a=sw_u=abc_w=42_c=1</c>.
///
/// Значения попадают в публичные ссылки, которые пользователи уже разослали,
/// поэтому менять сами токены можно только с обратной совместимостью:
/// старые ссылки должны продолжать открываться.
/// </summary>
public static class DeepLink
{
    /// <summary>Что открывает ссылка.</summary>
    public enum Kind
    {
        Wish,
        Friend,
        Broadcast,
        AdminUser,
    }

    /// <summary>
    /// Пользователь. В ссылках на виш и профиль — первые 20 символов
    /// <see cref="UserModel.SubscribeId"/>, в админской ссылке — внутренний идентификатор.
    /// </summary>
    public const string UserKey = "u";

    public const string WishIdKey = "w";
    public const string BroadcastIdKey = "i";

    /// <summary>Флаг «вернуться в список броней».</summary>
    public const string ReturnToClaimsKey = "c";

    private const string ActionKey = "a";
    private const string FlagYes = "1";
    private const char SegmentSeparator = '_';
    private const char KeyValueSeparator = '=';
    private const char ActionTextSeparator = ' ';

    public static string BuildWish(string subscribeId, int wishId, bool returnToClaims = false)
    {
        var payload = returnToClaims
            ? BuildPayload(Kind.Wish, (UserKey, subscribeId), (WishIdKey, wishId.ToString()), (ReturnToClaimsKey, FlagYes))
            : BuildPayload(Kind.Wish, (UserKey, subscribeId), (WishIdKey, wishId.ToString()));

        return BuildLink(payload);
    }

    public static string BuildFriend(string subscribeId) =>
        BuildLink(BuildPayload(Kind.Friend, (UserKey, subscribeId)));

    public static string BuildBroadcast(int broadcastId) =>
        BuildLink(BuildPayload(Kind.Broadcast, (BroadcastIdKey, broadcastId.ToString())));

    public static string BuildAdminUser(int userId) =>
        BuildLink(BuildPayload(Kind.AdminUser, (UserKey, userId.ToString())));

    /// <summary>
    /// Разбирает текст команды <c>/start a=...</c> на действие и параметры.
    /// Возвращает <see langword="false"/>, если это не ссылка бота.
    /// </summary>
    public static bool TryParse(string actionText, out Kind kind, out IReadOnlyDictionary<string, string> parameters)
    {
        kind = default;
        parameters = new Dictionary<string, string>();

        var parts = actionText.Split(ActionTextSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var parsed = new Dictionary<string, string>();

        foreach (var segment in parts[1].Split(SegmentSeparator))
        {
            var keyValue = segment.Split(KeyValueSeparator);
            if (keyValue.Length != 2)
                return false;

            if (!parsed.TryAdd(keyValue[0], keyValue[1]))
                return false;
        }

        if (!parsed.TryGetValue(ActionKey, out var actionToken) || !TryParseKind(actionToken, out kind))
            return false;

        parameters = parsed;
        return true;
    }

    private static string BuildLink(string payload) => $"t.me/{Config.Instance.Username}?start={payload}";

    private static string BuildPayload(Kind kind, params (string Key, string Value)[] segments)
    {
        var builder = new StringBuilder()
            .Append(ActionKey).Append(KeyValueSeparator).Append(ToToken(kind));

        foreach (var (key, value) in segments)
            builder.Append(SegmentSeparator).Append(key).Append(KeyValueSeparator).Append(value);

        return builder.ToString();
    }

    private static string ToToken(Kind kind) => kind switch
    {
        Kind.Wish => "sw",
        Kind.Friend => "fr",
        Kind.Broadcast => "br",
        Kind.AdminUser => "asu",
        _ => throw new NotSupportedException($"Unknown deep link kind '{kind}'"),
    };

    private static bool TryParseKind(string token, out Kind kind)
    {
        foreach (var candidate in Enum.GetValues<Kind>())
        {
            if (ToToken(candidate) == token)
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
