using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Xunit;

namespace Telecord.Tests
{
    public class EntitySlicingTests
    {
        private static Expectation<(int offset, int length)> Expect(MessageEntity entity, int start, int length)
        {
            var actual = entity.Slice(start, length);
            return new Expectation<(int offset, int length)>(expected => Assert.Equal(expected, (actual.Offset, actual.Length)));
        }

        [Fact]
        public void Inside()
        {
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 1, 10).ToBe((1, 3));
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 1, 5).ToBe((1, 3));
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 2, 3).ToBe((0, 3));
        }

        [Fact]
        public void TrimEnd()
        {
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 1, 3).ToBe((1, 2));
        }

        [Fact]
        public void TrimStart()
        {
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 3, 6).ToBe((0, 2));
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 3, 5).ToBe((0, 2));
        }

        [Fact]
        public void TrimBoth()
        {
            Expect(new MessageEntity { Offset = 2, Length = 3 }, 3, 1).ToBe((0, 1));
        }
    }
}
