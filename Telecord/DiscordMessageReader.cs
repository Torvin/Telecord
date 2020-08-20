using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Telecord
{
    using System.Linq;
    using Discord;
    using Discord.WebSocket;
    using DiscordParser;

    public class DiscordMessageReader
    {
        private static readonly Regex SpoilerRegex = new Regex(@"\S");

        private readonly IMessage _message;
        private readonly DiscordParser _parser = new DiscordParser();

        public DiscordMessageReader(IMessage message)
        {
            _message = message;
        }

        public TelegramMessage Read(DiscordSocketClient discord)
        {
            var message = ReadMessage(discord);

            foreach (var att in _message.Attachments)
                message.AppendUrl(att.Url);

            return message;
        }

        public string ReadSpoiler(DiscordSocketClient discord)
        {
            return ReadMessage(discord).Spoiler;
        }

        private TelegramMessage ReadMessage(DiscordSocketClient discord)
        {
            var message = new TelegramMessage(_message.Author.Username);
            HtmlVisitor.Convert(discord, _parser.Parse(_message.Content), message);
            return message;
        }

        class HtmlVisitor : Visitor
        {
            public static void Convert(DiscordSocketClient discord, Node node, TelegramMessage message)
            {
                var visitor = new HtmlVisitor(discord, message);
                visitor.Visit(node);
                visitor.UpdateMessage();
            }

            private readonly StringBuilder _builder = new StringBuilder();
            private readonly StringBuilder _spoilerBuilder = new StringBuilder();
            private int _plainTextLen;
            private DiscordSocketClient _discord;
            private readonly TelegramMessage _message;
            private bool _inSpoiler;
            private bool _hasSpoiler;

            public HtmlVisitor(DiscordSocketClient discord, TelegramMessage message)
            {
                _discord = discord;
                _message = message;
            }

            private void UpdateMessage()
            {
                _message.SetText(_builder.ToString(), _plainTextLen);
                _message.Spoiler = _hasSpoiler ? _spoilerBuilder.ToString() : null;
            }

            public override Node VisitChannel(ChannelNode channel)
            {
                Markup("<code>");
                Text($"#{((IChannel)_discord.GetChannel(channel.Id)).Name}");
                Markup("</code>");
                return channel;
            }

            public override Node VisitCode(CodeNode code)
            {
                Markup($"<pre><code{(code.Language != "" ? $" class=\"language-{code.Language}\"" : "")}>");
                Text(code.Code);
                Markup("</code></pre>");
                return code;
            }

            public override Node VisitCustomEmoji(CustomEmojiNode emoji)
            {
                Markup("<b>");
                Text($":{emoji.Name}");
                Markup("</b>");
                return emoji;
            }

            public override Node VisitLink(LinkNode link)
            {
                if (link.HidePreview)
                    _message.HidePreview();
                return Visit(() => base.VisitLink(link), $"<a href=\"{link.Url}\">", "</a>");
            }

            public override Node VisitMention(MentionNode mention)
            {
                var id =
                    mention.Id.HasValue ? _discord.GetUser(mention.Id.Value).Username :
                    mention.Special == SpecialMention.Everyone ? "everyone" :
                    mention.Special == SpecialMention.Here ? "here" :
                    throw new ArgumentOutOfRangeException("Unknown mention: " + mention.Special);

                Markup("<code>");
                Text($"@{id}");
                Markup("</code>");
                return mention;
            }

            public override Node VisitNewline(NewlineNode nl)
            {
                Text("\n");
                return nl;
            }

            public override Node VisitRole(RoleNode roleNode)
            {
                var role = _discord.Guilds.SelectMany(g => g.Roles).Single(r => r.Id == roleNode.Id);

                Markup("<code>");
                Text($"@{role.Name}");
                Markup("</code>");
                return roleNode;
            }

            public override Node VisitStyle(StyleNode style)
            {
                return style.Style switch
                {
                    Style.Bold => VisitTag("strong"),
                    Style.Italic => VisitTag("em"),
                    Style.Mono => VisitTag("code"),
                    Style.MultiMono => VisitTag("pre"),
                    Style.Strikethru => VisitTag("del"),
                    Style.Underline => VisitTag("u"),
                    Style.BlockQuote => VisitTag("pre"),
                    Style.InlineCode => VisitTag("code"),
                    Style.Spoiler => VisitSpoiler(style),

                    _ => throw new ArgumentException($"Unknown style: {style}"),
                };

                Node VisitTag(string tag) => Visit(() => base.VisitStyle(style), $"<{tag}>", $"</{tag}>");
            }

            private Node VisitSpoiler(StyleNode style)
            {
                _hasSpoiler = true;

                _inSpoiler = true;
                var node = base.VisitStyle(style);
                _inSpoiler = false;

                return node;
            }

            private Node Visit(Func<Node> visitNode, string open, string close)
            {
                Markup(open);
                var node = visitNode();
                Markup(close);
                return node;
            }

            public override Node VisitText(TextNode text)
            {
                Text(text.Text);
                return text;
            }

            private void Text(string text)
            {
                _plainTextLen += text.Length;

                if (_inSpoiler)
                    _builder.Append(SpoilerRegex.Replace(text, "█"));

                text = TelegramUtils.Escape(text).Replace("\u200b", "");

                _spoilerBuilder.Append(text);

                if (!_inSpoiler)
                    _builder.Append(text);
            }


            private void Markup(string text)
            {
                if (!_inSpoiler)
                   _builder.Append(text);
            }
        }
    }
}
