using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace Telecord.Tests
{
    public class TelegramMessageConverterTests
    {
        private static Expectation<string[]> Expect(int maxPartLength, string from, string text, params MessageEntity[] entities)
        {
            return Expect(maxPartLength, new Message
            {
                From = new User { Username = from },
                Text = text,
                Entities = entities,
            });
        }

        private static Expectation<string[]> Expect(int maxPartLength, Message message, string url = null)
        {
            var getFileUrl = url == null ? (TelegramMessageConverter.GetFileUrl)GetFileUrl : ((x, y, z, w) => url);
            var (actual, _) = new TelegramMessageConverter(maxPartLength, getFileUrl).Convert(message);
            return new Expectation<string[]>(expected => Assert.Equal(expected, actual));
        }

        private static string GetFileUrl(string fileId, string extension, string mimeType, string fileName)
        {
            return "URL";
        }

        [Fact]
        public void SplitInParagraphs()
        {
            Expect(13, "xx", "12 34\n56789").ToBe("**xx**: 12 34", "**xx**: 56789");
            Expect(17, "xx", "12 34\n56789").ToBe("**xx**: 12 34", "**xx**: 56789");
        }

        [Fact]
        public void SplitOnSpaces()
        {
            Expect(12, "xx", "12 34\n5678").ToBe("**xx**: 12", "**xx**: 34", "**xx**: 5678");
        }

        [Fact]
        public void SplitOnLetters()
        {
            Expect(12, "xx", "12 34\n56789").ToBe("**xx**: 12", "**xx**: 34", "**xx**: 5678", "**xx**: 9");
            Expect(12, "xx", "56789\n12 34").ToBe("**xx**: 5678", "**xx**: 9", "**xx**: 12", "**xx**: 34");
        }

        [Fact]
        public void SplitMarkup()
        {
            Expect(16, "xx", "12 34\n5678", new MessageEntity { Offset = 2, Length = 9, Type = MessageEntityType.Bold })
                .ToBe("**xx**: 12", "**xx**: **34**", "**xx**: **5678**");

            Expect(20, "xx", "12 34\n56789", new MessageEntity { Offset = 2, Length = 9, Type = MessageEntityType.Bold })
                .ToBe("**xx**: 12 **34**", "**xx**: **56789**");

            Expect(20, "xx", "12 34\n56789", new MessageEntity { Offset = 0, Length = 11, Type = MessageEntityType.Code })
                .ToBe("**xx**: ``12 34``", "**xx**: ``56789``");
        }

        [Fact]
        public void ThrowsWhenLimitIsTooSmall()
        {
            Assert.ThrowsAny<Exception>(() => Expect(9, "xx", "123456789", new MessageEntity { Offset = 2, Length = 9, Type = MessageEntityType.Bold }));
            Assert.ThrowsAny<Exception>(() => Expect(5, "xx", "123456789"));
            Assert.ThrowsAny<Exception>(() => Expect(7, "xx", "123456789"));
            Assert.ThrowsAny<Exception>(() => Expect(8, "xx", "123456789"));
        }

        [Fact]
        public void LinkInTheLastPart()
        {
            Expect(27, new Message
            {
                From = new User { Username = "xx" },
                Text = "1234567890xy\nabc456789",
                Animation = new Animation(),
            }).ToBe("**xx**: 1234567890xy", "**xx**: abc456789\nGIF: URL");

            Expect(25, new Message
            {
                From = new User { Username = "xx" },
                Text = "1234567890xy\nabc456789",
                Animation = new Animation(),
            }).ToBe("**xx**: 1234567890xy", "**xx**: abc456789", "**xx**: GIF: URL");
        }

        [Fact]
        public void Gif()
        {
            Expect(100, new Message
            {
                From = new User { Username = "xx" },
                Animation = new Animation(),
            }).ToBe("**xx**: GIF: URL");
        }

        [Fact]
        public void Photo()
        {
            Expect(100, new Message
            {
                From = new User { Username = "xx" },
                Photo = new[] { new PhotoSize() },
            }, "http://url/").ToBe("**xx**:");
        }

        [Fact]
        public void TrimQuoteWhitespace()
        {
            Expect(100, new Message
            {
                From = new User { Username = "xx" },
                Text = "zz",
                ReplyToMessage = new Message
                {
                    From = new User { Username = "yy" },
                    Text = new string('\n', 40) + "123456789x123456789",
                    Entities = new[] { new MessageEntity { Offset = 0, Length = 50, Type = MessageEntityType.Bold } }
                }
            }).ToBe("**xx**:\n> **123456789x**123456789\nzz");
        }

        [Fact]
        public void TrimQuote()
        {
            Expect(100, new Message
            {
                From = new User { Username = "xx" },
                Text = "zz",
                ReplyToMessage = new Message
                {
                    From = new User { Username = "yy" },
                    Text = new string('x', 40) + "123456789x123456789y123456789",
                    Entities = new[] { new MessageEntity { Offset = 0, Length = 60, Type = MessageEntityType.Bold } }
                }
            }).ToBe($"**xx**:\n> **{new string('x', 40)}1234567...**\nzz");
        }
    }
}
