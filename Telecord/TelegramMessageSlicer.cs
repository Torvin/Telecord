using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telecord
{
    public class TelegramMessageSlicer
    {
        public static (string, int) GetNextSlice(ReadOnlySpan<char> text, MessageEntity[] entities, int start, int maxLength)
        {
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException("maxLength", "Length can't be zero");

            while (start < text.Length && char.IsWhiteSpace(text[start]))
                start++;

            var length = Math.Min(maxLength, text.Length - start);
            if (length == 0)
                return (null, start);

            for (; ; )
            {
                ReadOnlySpan<char> span;

                if (length < text.Length - start)
                {
                    span = text.Slice(start, length + 1);

                    // trying to find a good boundary to split at
                    var index = span.LastIndexOf('\n');
                    if (index == -1)
                        index = span.LastIndexOf(' ');
                    if (index == -1)
                        index = length;

                    length = index;
                }

                span = text.Slice(start, length);

                var result = TelegramMessageReader.Read(span, entities.Slice(start, length), false);
                if (result.Length <= maxLength)
                    return (result, start + length);

                length--;

                if (length == 0)
                    throw new InvalidOperationException("maxLength can't be achieved");
            }
        }
    }
}
