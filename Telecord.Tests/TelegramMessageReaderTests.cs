using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace Telecord.Tests
{
    public class TelegramMessageReaderTests
    {
        private static Expectation<string> Expect(string text, params MessageEntity[] entities)
        {
            var actual = TelegramMessageReader.Read(text, entities, false);
            return new Expectation<string>(expected => Assert.Equal(expected, actual));
        }

        [Fact]
        public void LiftSpaces()
        {
            Expect("  123   ", new MessageEntity { Offset = 0, Length = 5, Type = MessageEntityType.Bold }).ToBe("  **123**   ");
            Expect("0 234 6",
                new MessageEntity { Offset = 0, Length = 7, Type = MessageEntityType.Italic },
                new MessageEntity { Offset = 1, Length = 5, Type = MessageEntityType.Bold }
            ).ToBe("*0* ***234*** *6*");

            Expect("0 234 6",
                new MessageEntity { Offset = 0, Length = 7, Type = MessageEntityType.Bold },
                new MessageEntity { Offset = 1, Length = 5, Type = MessageEntityType.Italic }
            ).ToBe("**0** ***234*** **6**");
        }

        [Fact]
        public void DontLiftCodeSpaces()
        {
            Expect(" 123 ", new MessageEntity { Offset = 0, Length = 5, Type = MessageEntityType.Code }).ToBe("`` 123 ``");
            Expect(" 123 ", new MessageEntity { Offset = 0, Length = 5, Type = MessageEntityType.Pre }).ToBe("``` 123 ```");
        }

        [Fact]
        public void BoldItalic()
        {
            Expect("0123456",
                new MessageEntity { Offset = 0, Length = 7, Type = MessageEntityType.Bold },
                new MessageEntity { Offset = 0, Length = 3, Type = MessageEntityType.Italic }
            ).ToBe("***012*3456**");
        }

        [Fact]
        public void ItalicBold()
        {
            Expect("0123456",
                new MessageEntity { Offset = 0, Length = 7, Type = MessageEntityType.Italic },
                new MessageEntity { Offset = 0, Length = 3, Type = MessageEntityType.Bold }
            ).ToBe("***012**3456*");
        }

        [Fact]
        public void VariousFormatting()
        {
            Expect("This is just an example with nested tags!",
                new MessageEntity { Offset = 5, Length = 2, Type = MessageEntityType.Bold },
                new MessageEntity { Offset = 13, Length = 2, Type = MessageEntityType.Italic },
                new MessageEntity { Offset = 15, Length = 8, Type = MessageEntityType.Code },
                new MessageEntity { Offset = 23, Length = 17, Type = MessageEntityType.Bold },
                new MessageEntity { Offset = 24, Length = 4, Type = MessageEntityType.Italic },
                new MessageEntity { Offset = 29, Length = 7, Type = MessageEntityType.Italic },
                new MessageEntity { Offset = 36, Length = 4, Type = MessageEntityType.Italic }
            ).ToBe(@"This **is** just *an*`` example`` ***with*** ***nested*** ***tags***!");
        }

        [Fact]
        public void CustomEmoji()
        {
            Expect("123\U0001f972456",
                new MessageEntity { Offset = 3, Length = 2, Type = MessageEntityType.CustomEmoji, CustomEmojiId = "5422352101885882552" }
            ).ToBe("123\U0001f972456");
        }

        [Fact]
        public void LinkSpaceLifting()
        {
            Expect(" please click here ",
                new MessageEntity { Offset = 0, Length = 19, Type = MessageEntityType.TextLink, Url = "http://blabla" },
                new MessageEntity { Offset = 7, Length = 7, Type = MessageEntityType.Bold }
            ).ToBe(" please **click** here  (http://blabla)");
        }

        [Fact]
        public void EscapeBackticks()
        {
            Expect("`12`34``5```6789`",
                new MessageEntity { Offset = 0, Length = 17, Type = MessageEntityType.Pre }
            ).ToBe("````\u200b12`34`\u200b`5`\u200b`\u200b`6789`\u200b```");
        }
    }
}
