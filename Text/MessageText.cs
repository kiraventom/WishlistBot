using System.Text;
using WishlistBot.Database.Users;

namespace WishlistBot.Text;

public class MessageText
{
   private readonly StringBuilder _sb = new();

   public MessageText(string text = null)
   {
      if (text is not null)
      {
         Verbatim(text);
      }
   }

   public MessageText LineBreak()
   {
      _sb.AppendLine();
      return this;
   }

   public MessageText Verbatim(string text)
   {
      if (text is null)
         return this;

      foreach (var ch in text)
      {
         var charCode = (int)ch;
         if (charCode is >= 1 and <= 126)
            _sb.Append('\\');

         _sb.Append(ch);
      }

      return this;
   }

   public MessageText Bold(string text)
   {
      _sb.Append('*');
      Verbatim(text);
      _sb.Append('*');

      return this;
   }

   public MessageText Italic(string text)
   {
      _sb.Append('_');
      Verbatim(text);
      _sb.Append('_');

      return this;
   }

   public MessageText ItalicBold(string text)
   {
      _sb.Append('*').Append('_');
      Verbatim(text);
      _sb.Append('_').Append('*');

      return this;
   }

   public MessageText Strikethrough(string text)
   {
      _sb.Append('~');
      Verbatim(text);
      _sb.Append('~');

      return this;
   }

   public MessageText Spoiler(string text)
   {
      _sb.Append("||");
      Verbatim(text);
      _sb.Append("||");

      return this;
   }

   public MessageText InlineUrl(string text, string link)
   {
      _sb.Append('[');
      Verbatim(text);
      _sb.Append(']');

      var escapedLink = EscapeLink(link);
      _sb.Append('(').Append(escapedLink).Append(')');
      return this;
   }

   public MessageText InlineUrl(string link)
   {
      var domainName = MessageTextUtils.GetDomainFromLink(link);
      domainName = char.ToUpper(domainName[0]) + domainName[1..];
      return InlineUrl(domainName, link);
   }

   public MessageText InlineMention(BotUser user)
   {
      return string.IsNullOrEmpty(user.Tag)
         ? InlineMention(user.FirstName, user.SenderId)
         : InlineMention(user.FirstName, user.Tag);
   }

   public MessageText InlineMention(string text, string tag)
   {
      _sb.Append('[');
      Verbatim(text);
      _sb.Append(']');

      _sb.Append('(')
         .Append(@"t.me/")
         .Append(tag)
         .Append(')');

      return this;
   }

   public MessageText InlineMention(string text, long userId)
   {
      _sb.Append('[');
      Verbatim(text);
      _sb.Append(']');

      _sb.Append('(')
         .Append(@"tg://user?id=")
         .Append(userId)
         .Append(')');

      return this;
   }

   public MessageText Monospace(string text)
   {
      _sb.Append('`');
      Verbatim(text);
      _sb.Append('`');

      return this;
   }

   public MessageText Quote(string text)
   {
      // Empty bold definition to separate from previous quote
      _sb.Append("**");
      var lines = text.Split('\n');

      for (var i = 0; i < lines.Length; ++i)
      {
         var line = lines[i];
         _sb.Append('>');
         Verbatim(line);

         if (i < lines.Length - 1)
            _sb.AppendLine();
      }

      return this;
   }

   public MessageText ExpandableQuote(string text)
   {
      Quote(text);

      _sb.Append("||");
      return this;
   }

   public override string ToString() => _sb.ToString();

   private static string EscapeLink(string link) => link.Replace("\\", @"\\").Replace(")", "\\)");
}
