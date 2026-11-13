using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.Keyboard;

public class BotKeyboard()
{
    private readonly List<List<IBotButton>> _rows = [];
    private QueryParameterCollection _commonParameters = new();

    /// <summary>
    /// Callback-данные кнопок, которые живут в тексте сообщения (&lt;tg-button&gt;),
    /// а не в reply_markup. В клавиатуру они не попадают, но экран должен их разрешить.
    /// </summary>
    private readonly List<string> _textQueries = [];

    public IEnumerable<string> EnumerateQueries()
    {
        var keyboardQueries = this.ToInlineKeyboardMarkup().InlineKeyboard
           .SelectMany(b => b)
           .Select(b => b.CallbackData);

        return _textQueries.Concat(keyboardQueries);
    }

    /// <summary>
    /// Кнопка внутри текста: возвращает callback-данные, которые экран передаёт
    /// в <c>MessageText.Button</c>. Регистрируется как разрешённый запрос экрана.
    /// </summary>
    public string AddTextButton<T>(string caption = null, params QueryParameter[] parameters) where T : IQuery, new()
    {
        var query = new T();
        var mergedParameters = QueryParameterCollection.Merge(_commonParameters, new QueryParameterCollection(parameters));
        var data = QueryUtils.BuildQueryStr(query.Data, mergedParameters);

        _textQueries.Add(data);

        return data;
    }

    public void InitCommonParameters(QueryParameterCollection commonParameters) => _commonParameters = commonParameters;

    public BotKeyboard AddButton<T>(string customCaption = null) where T : IQuery, new() => AddButton<T>(customCaption, []);

    public BotKeyboard AddButton<T>(params QueryParameter[] parameters) where T : IQuery, new() => AddButton<T>(null, parameters);

    public BotKeyboard AddButton(string data, string caption, IEnumerable<QueryParameter> parameters = null) => AddButton(new BotButton(data, caption, parameters ?? []));

    public BotKeyboard AddCopyTextButton(string caption, string textToCopy) => AddButton(new CopyTextBotButton(caption, textToCopy));

    public BotKeyboard AddShareButton(string caption, string textToShare) => AddButton(new ShareBotButton(caption, textToShare));

    public BotKeyboard AddButton<T>(string customCaption, params QueryParameter[] parameters) where T : IQuery, new()
    {
        var query = new T();
        var caption = customCaption ?? query.Caption;

        return AddButton(query.Data, caption, parameters);
    }

    public BotKeyboard NewRow()
    {
        _rows.Add([]);
        return this;
    }

    public InlineKeyboardMarkup ToInlineKeyboardMarkup()
    {
        var markupRows = _rows
           .Where(r => r.Count != 0)
           .Select(r => r.Select(b => b.ToInlineKeyboardButton(_commonParameters)));

        return new InlineKeyboardMarkup() { InlineKeyboard = markupRows };
    }

    private BotKeyboard AddButton(IBotButton button)
    {
        var lastRow = _rows.LastOrDefault();
        if (lastRow is null)
        {
            lastRow = [];
            _rows.Add(lastRow);
        }

        lastRow.Add(button);
        return this;
    }
}
