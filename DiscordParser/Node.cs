using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordParser
{
    public abstract class Node
    {
        protected Node(IEnumerable<Node> children)
        {
            Children = children.ToArray();
        }

        protected Node(params Node[] children)
            : this(children.AsEnumerable())
        {
        }

        public Node[] Children { get; }
    }

    public class ContainerNode : Node
    {
        public ContainerNode(IEnumerable<Node> children)
            : base(children)
        {
        }
    }

    public class TextNode : Node
    {
        public TextNode(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public class NewlineNode : Node
    {
    }

    public class StyleNode : Node
    {
        public StyleNode(Style style, Node child)
            : base(child)
        {
            Style = style;
        }

        public Style Style { get; }
        public Node Child => Children.Single();
    }

    public enum Style
    {
        Bold,
        Italic,
        Underline,
        Strikethru,
        Mono,
        MultiMono,
        BlockQuote,
        InlineCode,
        Spoiler,
    }

    public class CodeNode : Node
    {
        public CodeNode(string language, string code)
        {
            Language = language;
            Code = code;
        }

        public string Language { get; }
        public string Code { get; }
    }

    public class LinkNode : Node
    {
        public LinkNode(string url, Node content = null, bool hidePreview = false)
            : base(content ?? new TextNode(url))
        {
            Url = url;
            HidePreview = hidePreview;
        }

        public string Url { get; }
        public bool HidePreview { get; }
        public Node Content => Children.Single();
    }

    public class MentionNode : Node
    {
        public MentionNode(ulong id)
        {
            Id = id;
        }

        public MentionNode(SpecialMention special)
        {
            Special = special;
        }

        public ulong? Id { get; }
        public SpecialMention? Special { get; }
    }

    public enum SpecialMention
    {
        Everyone,
        Here,
    }

    public class ChannelNode : Node
    {
        public ChannelNode(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; }
    }

    public class RoleNode : Node
    {
        public RoleNode(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; }
    }

    public class CustomEmojiNode : Node
    {
        public CustomEmojiNode(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public abstract class Visitor
    {
        public Node Visit(Node node)
        {
            return node switch
            {
                TextNode text => VisitText(text),
                StyleNode style => VisitStyle(style),
                NewlineNode nl => VisitNewline(nl),
                CodeNode code => VisitCode(code),
                LinkNode link => VisitLink(link),
                MentionNode mention => VisitMention(mention),
                ChannelNode channel => VisitChannel(channel),
                RoleNode role => VisitRole(role),
                CustomEmojiNode emoji => VisitCustomEmoji(emoji),
                ContainerNode container => VisitContainer(container),

                _ => throw new ArgumentException($"Unknown node type: {node.GetType()}."),
            };
        }

        public virtual Node VisitNewline(NewlineNode nl) => nl;
        public virtual Node VisitText(TextNode text) => text;
        public virtual Node VisitMention(MentionNode mention) => mention;
        public virtual Node VisitChannel(ChannelNode channel) => channel;
        public virtual Node VisitRole(RoleNode role) => role;
        public virtual Node VisitCustomEmoji(CustomEmojiNode emoji) => emoji;
        public virtual Node VisitCode(CodeNode code) => code;

        public virtual Node VisitLink(LinkNode link)
        {
            var child = Visit(link.Content);
            if (child == link.Content)
                return link;
            else
                return new LinkNode(link.Url, child);
        }

        public virtual Node VisitStyle(StyleNode style)
        {
            var child = Visit(style.Child);
            if (child == style.Child)
                return style;
            else
                return new StyleNode(style.Style, child);
        }

        public virtual Node VisitContainer(ContainerNode parent)
        {
            var children = VisitChildren(parent.Children);
            if (children == parent.Children)
                return parent;
            else
                return new ContainerNode(children);
        }

        protected IReadOnlyList<Node> VisitChildren(IReadOnlyList<Node> nodes)
        {
            var list = new Node[nodes.Count];
            var changed = false;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var newNode = list[i] = Visit(node);

                if (newNode != node)
                    changed = true;
            }

            return changed ? list : nodes;
        }
    }
}
