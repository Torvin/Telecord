using System;
using Xunit;

namespace DiscordParser.Tests
{
    public class ParserTests
    {
        private void Test(string markdown, string html)
        {
            var node = new DiscordParser().Parse(markdown);
            var actual = new HtmlWriter().Write(node);

            Assert.Equal(html, actual);
        }

        [Fact(DisplayName = "Converts **text** to <strong>text</strong>")]
        public void Strong()
        {
            Test("This is a **test** with **some bold** text in it", "This is a <strong>test</strong> with <strong>some bold</strong> text in it");
        }

        [Fact(DisplayName = "Converts _text_ to <em>text</em>")]
        public void Em()
        {
            Test("This is a _test_ with _some italicized_ text in it", "This is a <em>test</em> with <em>some italicized</em> text in it");
        }

        [Fact(DisplayName = "Converts _ text_ to <em> text </em>")]
        public void Em2()
        {
            Test("This is a _ test_ with _ italic _ text in it", "This is a <em> test</em> with <em> italic </em> text in it");
        }

        [Fact(DisplayName = "Converts __text__ to <u>text</u>")]
        public void Underline()
        {
            Test("This is a __test__ with __some underlined__ text in it", "This is a <u>test</u> with <u>some underlined</u> text in it");
        }

        [Fact(DisplayName = "Converts *text* to <em>text</em>")]
        public void Em3()
        {
            Test("This is a *test* with *some italicized* text in it", "This is a <em>test</em> with <em>some italicized</em> text in it");
        }

        [Fact(DisplayName = "Converts `text` to <code>text</code>")]
        public void Code()
        {
            Test("Code: `1 + 1 = 2`", "Code: <code>1 + 1 = 2</code>");
        }

        [Fact(DisplayName = "Converts ~~text~~ to <del>text</del>")]
        public void Strike()
        {
            Test("~~this~~that", "<del>this</del>that");
        }

        [Fact(DisplayName = "Converts ~~ text ~~ to <del>")]
        public void Strike2()
        {
            Test("~~ text ~~ stuffs", "<del> text </del> stuffs");
        }

        [Fact(DisplayName = "Converts links to <a> links")]
        public void Links()
        {
            Test("https://brussell.me", "<a href=\"https://brussell.me\">https://brussell.me</a>");
            Test("xhttps://brussell.me", "x<a href=\"https://brussell.me\">https://brussell.me</a>");
        }

        [Fact]
        public void Autolinks()
        {
            Test("<https://brussell.me>", "<a href=\"https://brussell.me\">https://brussell.me</a>");
            Test("<xhttps://brussell.me>", "<a href=\"xhttps://brussell.me\">xhttps://brussell.me</a>");
        }

        [Fact(DisplayName = "Fence normal code blocks")]
        public void MultiCode()
        {
            Test("text\n```\ncode\nblock\n```\nmore text", "text\n<pre><code class=\"hljs\">code\nblock</code></pre>\nmore text");
        }

        [Fact(DisplayName = "Fenced code blocks with hljs")]
        public void MultiCode2()
        {
            Test("```js\nconst one = 1;\nconsole.log(one);\n```", "<pre><code class=\"hljs js\">const one = 1;\nconsole.log(one);</code></pre>");
        }

        [Fact(DisplayName = "Fenced code blocks on one line")]
        public void Code2()
        {
            Test("`test`\n\n```test```", "<code>test</code>\n\n<pre><code class=\"hljs\">test</code></pre>");
        }

        [Fact]
        public void Multiline()
        {
            Test("multi\nline", "multi\nline");
            Test("some *awesome* text\nthat **spreads** lines", "some <em>awesome</em> text\nthat <strong>spreads</strong> lines");
        }

        [Fact(DisplayName = "Block quotes")]
        public void Quotes()
        {
            Test("> text > here", "<blockquote>text &gt; here</blockquote>");
            Test("> text\nhere", "<blockquote>text\n</blockquote>here");
            Test(">text", "&gt;text");
            Test("outside\n>>> inside\ntext\n> here\ndoes not end", "outside\n<blockquote>inside\ntext\n&gt; here\ndoes not end</blockquote>");
            Test(">>> test\n```js\ncode```", "<blockquote>test\n<pre><code class=\"hljs js\">code</code></pre></blockquote>");
            Test("> text\n> \n> here", "<blockquote>text\n\nhere</blockquote>");
        }

        [Fact]
        public void BoldInQuote()
        {
            Test("> **bold**\nend", "<blockquote><strong>bold</strong>\n</blockquote>end");
            Test(">>> **bold**\nend", "<blockquote><strong>bold</strong>\nend</blockquote>");
        }

        [Fact(DisplayName = "Don't drop arms")]
        public void Kaomoji()
        {
            Test("¯\\_(ツ)_/¯", "¯\\_(ツ)_/¯");
            Test("¯\\_(ツ)_/¯ *test* ¯\\_(ツ)_/¯", "¯\\_(ツ)_/¯ <em>test</em> ¯\\_(ツ)_/¯");
        }

        [Fact(DisplayName = "Only embeds have [label](link)")]
        public void Link()
        {
            Test("[label](http://example.com)", "[label](<a href=\"http://example.com\">http://example.com</a>)");
        }

        [Fact(DisplayName = "Escape html")]
        public void Html()
        {
            Test("<b>test</b>", "&lt;b&gt;test&lt;/b&gt;");
        }

        [Fact(DisplayName = "Unmatched backtick")]
        public void UnmatchedBactick()
        {
            Test("`Inline `code` with extra marker", "<code>Inline </code>code` with extra marker");
        }

        [Fact(DisplayName = "* next to space")]
        public void StarSpace()
        {
            Test("*Hello World! *", "*Hello World! *");
        }

        [Fact(DisplayName = "Triple *s")]
        public void TripleStar()
        {
            Test("***underlined bold***", "<em><strong>underlined bold</strong></em>");
        }

        [Fact(DisplayName = "Inline code with ` inside")]
        public void DoubleBackticks()
        {
            Test(@"``function test() { return ""`"" }``", "<code>function test() { return \"`\" }</code>");
        }

        [Fact(DisplayName = "Code blocks aren't parsed")]
        public void NoSpaceCodeblocks()
        {
            Test("some\n    text", "some\n    text");
        }

        [Fact(DisplayName = "Multiple new lines")]
        public void MultiNewLine()
        {
            Test("some\n\ntext", "some\n\ntext");
        }

        [Fact(DisplayName = "No undserscore italic in one word")]
        public void Undescores()
        {
            Test("test_ing_stuff", "test_ing_stuff");
        }

        [Fact(DisplayName = "Spoiler edge-cases")]
        public void SpoilerEdgeCases()
        {
            Test("||||", "||||");
            Test("|| ||", @"<span class=""d-spoiler""> </span>");
            Test("||||||", @"<span class=""d-spoiler"">|</span>|");
        }

        [Fact(DisplayName = "Spoilers are handled correctly")]
        public void Spoilers()
        {
            Test("||spoiler||", "<span class=\"d-spoiler\">spoiler</span>");
            Test("|| spoiler ||", "<span class=\"d-spoiler\"> spoiler </span>");
            Test("|| spoiler | message ||", "<span class=\"d-spoiler\"> spoiler | message </span>");
            Test("a ||spoiler|| may have ||multiple\nlines||", "a <span class=\"d-spoiler\">spoiler</span> may have <span class=\"d-spoiler\">multiple\nlines</span>");
            Test("||strange||markdown||", "<span class=\"d-spoiler\">strange</span>markdown||");
            Test("||<i>itallics</i>||", "<span class=\"d-spoiler\">&lt;i&gt;itallics&lt;/i&gt;</span>");
            Test("||```\ncode\nblock\n```||", "<span class=\"d-spoiler\"><pre><code class=\"hljs\">code\nblock</code></pre></span>");
        }

        [Fact(DisplayName = "Nested <em>")]
        public void NestedEms()
        {
            Test("_hello world *foo bar* hello world_", "<em>hello world foo bar hello world</em>");
            Test("_hello world *foo __blah__ bar* hello world_", "<em>hello world foo <u>blah</u> bar hello world</em>");
            Test("_hello world __foo *blah* bar__ hello world_", "<em>hello world <u>foo blah bar</u> hello world</em>");
            Test("_hello *world*_ not em *foo*", "<em>hello world</em> not em <em>foo</em>");
        }

        [Fact(DisplayName = "User parsing")]
        public void UserParsing()
        {
            Test("hey <@1234>!", "hey <span class=\"d-mention d-user\">@1234</span>!");
        }

        [Fact(DisplayName = "Role parsing")]
        public void RoleParsing()
        {
            Test("is any of <@&1234> here?", "is any of <span class=\"d-mention d-role\">&1234</span> here?");
        }

        [Fact(DisplayName = "Channel parsing")]
        public void ChannelParsing()
        {
            Test("goto <#1234>, please", "goto <span class=\"d-mention d-channel\">#1234</span>, please");
        }

        [Fact(DisplayName = "@everyone parsing")]
        public void EveryoneParsing()
        {
            Test("hey @everyone!", "hey <span class=\"d-mention d-user\">@everyone</span>!");
        }

        [Fact(DisplayName = "@here parsing")]
        public void HereParsing()
        {
            Test("hey @here!", "hey <span class=\"d-mention d-user\">@here</span>!");
        }

        [Fact(DisplayName = "Don't parse stuff in code blocks")]
        public void NoMentionsInCode()
        {
            Test("`<@1234>`", "<code>&lt;@1234&gt;</code>");
        }

        [Fact(DisplayName = "Custom emojis")]
        public void CustomEmojis()
        {
            Test("heh <:blah:1234>", "heh <span class=\"d-emoji\">:blah:</span>");
            Test("heh <a:blah:1234>", "heh <span class=\"d-emoji\">:blah:</span>");
        }

        [Fact]
        public void Newline()
        {
            Test("1\n \n2\n  \n3\n   \n4", "1\n \n2\n<br>3\n<br>4");
        }

        [Fact]
        public void WeirdFormatting()
        {
            Test("*a*b*", "<em>a</em>b*");
            Test("***aaa*b**", "<strong><em>aaa</em>b</strong>");
            Test("***another test***", "<em><strong>another test</strong></em>");
            Test("***this *is* a test***", "<strong>*this <em>is</em> a test*</strong>");
            Test("**_this _is_ a test_**", "<strong>_this <em>is</em> a test_</strong>");
            Test("**_this_ is _a test_**", "<strong><em>this</em> is <em>a test</em></strong>");

            Test("*aa *bb cc*", "*aa <em>bb cc</em>");
            Test("**aa **bb cc**", "<strong>aa </strong>bb cc**");
            Test("*aa *bb cc", "*aa *bb cc");
        }

        [Fact]
        public void Escape()
        {
            Test("**bold \\**test**", "<strong>bold **test</strong>");
        }

        [Fact]
        public void TagsInCode()
        {
            Test("```<b>haha</b>```", "<pre><code class=\"hljs\">&lt;b&gt;haha&lt;/b&gt;</code></pre>");
        }
    }
}
