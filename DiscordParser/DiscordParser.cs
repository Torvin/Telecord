using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordParser
{
    public class DiscordParser : Parser<DiscordParserState>
    {
        private static readonly Regex PreBlockQuote = new Regex(@"^$|\n *$");
        private static readonly Regex BlockQuoteSingular = new Regex("^ *> ?", RegexOptions.Multiline);
        private static readonly Regex BlockQuoteTriple = new Regex("^ *>>> ?");

        //private static readonly string LinkTitleRegex = @"(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*";
        //private static readonly string LinkHrefRegex = @"\s*<?((?:\([^)]*\)|[^\s\\]|\\.)*?)>?(?:\s+['\""]([\s\S]*?)['\""])?\s*";
        //private static readonly Regex UrlUnescapeRegex = new Regex(@"\\([^0-9A-Za-z\s])");
        //private static readonly Regex HttpUrlRegex = new Regex(@"^https?:\/\/", RegexOptions.IgnoreCase);

        private static readonly string[] BadProtocols = { "file:", "javascript:", "vbscript:", "data:" };

        private static readonly Rule[] Rules = {
            // newlines
            new Rule(new Regex(@"^(?:\n *)*\n", RegexOptions.Singleline))
            {
                Block = true,
                Parse = (parser, state) => new TextNode("\n"),
                Order = 10,
            },

            // br
            new Rule(new Regex(@"^ {2,}\n"))
            {
                Parse = (parser, state) => new NewlineNode(),
                Order = 24,
            },

            // escape
            new Rule(new Regex(@"^\\([^0-9A-Za-z\s])"))
            {
                Block = false,
                Parse = (parser, state) => new TextNode(state.LastMatch.Groups[1].Value),
                Order = 12,
            },

            // blockquote
            new Rule(new Regex(@"^( *>>> +([\s\S]*))|^( *>(?!>>) +[^\n]*(\n *>(?!>>) +[^\n]*)*\n?)"))
            {
                Match = (text, parser, state, match) => !PreBlockQuote.IsMatch(state.LastMatch?.Value ?? "") || state.InQuote || state.Nested ? null : match(text),
                Parse = (parser, state) =>
                {
                    var regex = BlockQuoteTriple.IsMatch(state.LastMatch.Value) ? BlockQuoteTriple : BlockQuoteSingular;
                    var quote = regex.Replace(state.LastMatch.Value, "");
                    state.InQuote = true;
                    var parsed = parser.Parse(quote, state);
                    return parsed != null ? new StyleNode(Style.BlockQuote, parsed) : (Node)new TextNode(" ");
                },
                Order = 6,
            },

            // bold
            new Rule(new Regex(@"^\*\*((?:\\[\s\S]|[^\\])+?)\*\*(?!\*)"))
            {
                Block = false,
                Parse = (parser, state) => StyleNode(state.LastMatch.Groups[1].Value, Style.Bold, parser, state),
                Quality = (match, state) => match.Value.Length + 0.1,
                Order = 21,
            },

            // underline
            new Rule(new Regex(@"^__([\s\S]+?)__(?!_)"))
            {
                Block = false,
                Parse = (parser, state) => StyleNode(state.LastMatch.Groups[1].Value, Style.Underline, parser, state),
                Quality = (match, state) => match.Value.Length,
                Order = 21,
            },

            // italics
            new Rule(new Regex(
                    // only match _s surrounding words.
                    "^\\b_" + "((?:__|\\\\[\\s\\S]|[^\\\\_])+?)_" + "\\b" +
                    "|" +
                    // Or match *s that are followed by a non-space:
                    "^\\*(?=\\S)(" +
                    // Match any of:
                    //  - `**`: so that bolds inside italics don't close the
                    // italics
                    //  - whitespace
                    //  - non-whitespace, non-* characters
                    "(?:\\*\\*|\\s+(?:[^*\\s]|\\*\\*)|[^\\s*])+?" +
                    // followed by a non-space, non-* then *
                    ")\\*(?!\\*)"
                ))
            {
                Block = false,
                Parse = (parser, state) =>
                {
                    var match = state.LastMatch;
                    var group = match.Groups[2].Success ? match.Groups[2] : match.Groups[1];
                    return StyleNode(group.Value, Style.Italic, parser, state);
                },
                Quality = (match, state) => match.Value.Length + 0.2,
                Order = 21,
            },

            // strikethru
            new Rule(new Regex(@"^~~([\s\S]+?)~~(?!_)"))
            {
                Block = false,
                Parse = (parser, state) => StyleNode(state.LastMatch.Groups[1].Value, Style.Strikethru, parser, state),
                Order = 21,
            },

            // text
            new Rule(new Regex(@"^[\s\S]+?(?=[^0-9A-Za-z\s\u00c0-\uffff]|\n\n| {2,}\n|\w+:\S|$)"))
            {
                Parse = (parser, state) => new TextNode(state.LastMatch.Groups[0].Value),
                Order = 32,
            },

            // inline code
            new Rule(new Regex(@"^(`+)([\s\S]*?[^`])\1(?!`)"))
            {
                Block = false,
                Parse = (parser, state) => new StyleNode(Style.InlineCode, new TextNode(state.LastMatch.Groups[2].Value)),
                Order = 23,
            },

            // code block
            new Rule(new Regex(@"^```(([a-z0-9_+\-.]+?)\n)?\n*([^\n](.|\n)*?)\n*```", RegexOptions.IgnoreCase))
            {
                Parse = (parser, state) => new CodeNode(state.LastMatch.Groups[2].Value.Trim(), state.LastMatch.Groups[3].Value),
                Order = 4,
            },

            // kaomoji
            new Rule(new Regex(@"^(¯\\_\(ツ\)_\/¯)"))
            {
                Parse = (parser, state) => new TextNode(state.LastMatch.Groups[1].Value),
                Order = 27,
            },

            // link
            //new Rule(new Regex(@"^\\[(" + LinkTitleRegex + ")\\]\\(" + LinkHrefRegex + "\\)"))
            //{
            //    Order = 17,
            //    Parse = (parser, state) =>
            //    {
            //        var url = UnescapeUrl(state.LastMatch.Groups[2].Value);
            //        if (!HttpUrlRegex.IsMatch(url))
            //        {

            //        }
            //        return null;
            //    },
            //},

            // autolink
            new Rule(new Regex(@"^<([^: >]+:\/[^ >]+)>"))
            {
                Block = false,
                Parse = (parser, state) =>
                {
                    var url = state.LastMatch.Groups[1].Value;
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || BadProtocols.Contains(uri.Scheme))
                        return new TextNode(url);

                    return new LinkNode(url, hidePreview: true);
                },
                Order = 14,
            },

            // url
            new Rule(new Regex(@"^(https?:\/\/[^\s<]+[^<.,:;""')\]\s])"))
            {
                Block = false,
                Parse = (parser, state) => new LinkNode(state.LastMatch.Groups[1].Value),
                Order = 16,
            },

            // spoiler
            new Rule(new Regex(@"^\|\|([\s\S]+?)\|\|"))
            {
                Parse = (parser, state) => StyleNode(state.LastMatch.Groups[1].Value, Style.Spoiler, parser, state),
                Order = 31,
            },

            // mention
            new Rule(new Regex(@"^<@!?(\d+)>|^(?:@(everyone|here))"))
            {
                Parse = (parser, state) => state.LastMatch.Groups[1].Success
                    ? new MentionNode(ulong.Parse(state.LastMatch.Groups[1].Value))
                    : new MentionNode((SpecialMention)Enum.Parse(typeof(SpecialMention), state.LastMatch.Groups[2].Value, true)),
                Order = 29,
            },

            // channel
            new Rule(new Regex(@"^<#(\d+)>"))
            {
                Parse = (parser, state) => new ChannelNode(ulong.Parse(state.LastMatch.Groups[1].Value)),
                Order = 25,
            },

            // role
            new Rule(new Regex(@"^<@&(\d+)>"))
            {
                Parse = (parser, state) => new RoleNode(ulong.Parse(state.LastMatch.Groups[1].Value)),
                Order = 30,
            },

            // custom emoji
            new Rule(new Regex(@"^<a?:(\w+):(\d+)>"))
            {
                Parse = (parser, state) => new CustomEmojiNode(state.LastMatch.Groups[1].Value),
                Order = 5,
            },
        };

        //private static string UnescapeUrl(string url)
        //{
        //    return UrlUnescapeRegex.Replace(url, "$1");
        //}

        private static Node StyleNode(string text, Style style, Parser<DiscordParserState> parser, DiscordParserState state)
        {
            int bit = 1 << (int)style;

            if ((state.Style & bit) != 0)
                return parser.Parse(text, state);

            state.Style |= bit;
            return new StyleNode(style, parser.Parse(text, state));
        }

        public DiscordParser()
            : base(Rules)
        {
        }

        class Rule : Rule<DiscordParserState>
        {
            public Rule(Regex regex)
            {
                base.Match = (text, parser, state) =>
                    Block == null || Block == (state.LastMatch?.Value.EndsWith("\n") ?? false)
                        ? Match(text, (DiscordParser)parser, state, str => regex.Match(str))
                        : null;

                Match = (text, parser, state, match) => match(text);
            }

            public new Func<string, DiscordParser, DiscordParserState, Func<string, Match>, Match> Match { get; set; }

            public bool? Block { get; set; }
        }
    }

    public struct DiscordParserState : IParserState
    {
        public Match LastMatch { get; set; }
        public bool InQuote { get; set; }
        public bool Nested { get; set; }
        public int Style { get; set; }
    }
}
