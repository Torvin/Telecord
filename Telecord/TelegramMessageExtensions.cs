using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telecord
{
    public static class TelegramMessageExtensions
    {
        public static MessageEntity[] Slice(this MessageEntity[] entities, int start, int length)
        {
            var end = start + length;
            return entities
                .Where(e => e.Offset < end && start < e.GetFinish())
                .Select(e => e.Slice(start, length))
                .ToArray();

        }

        public static MessageEntity Slice(this MessageEntity e, int start, int length)
        {
            var newOffset = e.Offset - start;
            var newLength = e.Length;

            if (newOffset < 0)
            {
                newLength += newOffset;
                newOffset = 0;
            }

            if (newOffset + newLength > length)
                newLength = length - newOffset;

            if (e.Offset == newOffset && e.Length == newLength) return e;
            return new MessageEntity { Offset = newOffset, Length = newLength, Type = e.Type, Url = e.Url, User = e.User };
        }

        public static int GetFinish(this MessageEntity entity)
        {
            return entity.Offset + entity.Length;
        }

        public static (string text, MessageEntity[] entities) GetTextAndEntities(this Message message)
        {
            return (message.Text ?? message.Caption, message.Entities ?? message.CaptionEntities ?? new MessageEntity[0]);
        }
    }
}
