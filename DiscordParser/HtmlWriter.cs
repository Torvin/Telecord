using System;
using System.Text;

namespace DiscordParser
{
    public class HtmlWriter
    {
        public string Write(Node node)
        {
            var visitor = new HtmlVisitor();
            visitor.Visit(node);
            return visitor.ToString();
        }

        class HtmlVisitor : Visitor
        {
            private readonly StringBuilder _builder = new StringBuilder();

            public override Node VisitChannel(ChannelNode channel)
            {
                _builder.Append($"<span class=\"d-mention d-channel\">#{channel.Id}</span>");
                return channel;
            }

            public override Node VisitCode(CodeNode code)
            {
                _builder.Append($"<pre><code class=\"hljs{(code.Language != "" ? " " + code.Language : "")}\">");
                AppendText(code.Code);
                _builder.Append("</code></pre>");
                return code;
            }

            public override Node VisitCustomEmoji(CustomEmojiNode emoji)
            {
                _builder.Append($"<span class=\"d-emoji\">:{emoji.Name}:</span>");
                return emoji;
            }

            public override Node VisitLink(LinkNode link)
            {
                return Visit(() => base.VisitLink(link), $"<a href=\"{link.Url}\">", "</a>");
            }

            public override Node VisitMention(MentionNode mention)
            {
                var id =
                    mention.Id.HasValue ? mention.Id.ToString() :
                    mention.Special == SpecialMention.Everyone ? "everyone" :
                    mention.Special == SpecialMention.Here ? "here" :
                    throw new ArgumentOutOfRangeException("Unknown mention: " + mention.Special);

                _builder.Append($"<span class=\"d-mention d-user\">@{id}</span>");
                return mention;
            }

            public override Node VisitNewline(NewlineNode nl)
            {
                _builder.Append("<br>");
                return nl;
            }

            public override Node VisitRole(RoleNode role)
            {
                _builder.Append($"<span class=\"d-mention d-role\">&{role.Id}</span>");
                return role;
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
                    Style.BlockQuote => VisitTag("blockquote"),
                    Style.InlineCode => VisitTag("code"),
                    Style.Spoiler => Visit(() => base.VisitStyle(style), "<span class=\"d-spoiler\">", "</span>"),

                    _ => throw new ArgumentException($"Unknown style: {style}"),
                };

                Node VisitTag(string tag) => Visit(() => base.VisitStyle(style), $"<{tag}>", $"</{tag}>");
            }

            private Node Visit(Func<Node> visitNode, string open, string close)
            {
                _builder.Append(open);
                var node = visitNode();
                _builder.Append(close);
                return node;
            }

            public override Node VisitText(TextNode text)
            {
                AppendText(text.Text);
                return text;
            }

            private void AppendText(string text)
            {
                _builder.Append(text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
            }

            public override string ToString()
            {
                return _builder.ToString();
            }
        }
    }
}
