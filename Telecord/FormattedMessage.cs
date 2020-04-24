using System;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.Enums;

namespace Telecord
{
    class FormattedMessage
    {
        private readonly Telegram.Bot.Types.Message _message;

        public FormattedMessage(Telegram.Bot.Types.Message telegramMessage)
        {
            _message = telegramMessage;
        }
        /*
        public string Format(IMessageWriter formatter)
        {
            var sb = new StringBuilder();
            var text = _message.Text.AsSpan();
            var offset = 0;

            for (var i = 0; i < Entities.Length; i++)
            {
                var entity = Entities[i];
                if (offset < entity.Offset)
                {
                    sb.Append(formatter.Text(text.Slice(offset, entity.Offset - offset)));
                    offset = entity.Offset;
                }

                var next = (i + 1 < Entities.Length ? Entities[i + 1].Offset : text.Length) - entity.Offset;
                var span = text.Slice(offset, Math.Min(next, entity.Length));

                switch (entity.Type)
                {
                    case MessageEntityType.Bold:
                        sb.Append(formatter.Bold(span));
                        break;

                    case MessageEntityType.Italic:
                        sb.Append(formatter.Italic(span));
                        break;

                    case MessageEntityType.Code:
                        sb.Append(formatter.Mono(span));
                        break;

                    case MessageEntityType.Pre:
                        sb.Append(formatter.MultiMono(span));
                        break;

                    default:
                        sb.Append(formatter.Text(span));
                        break;
                }

                offset += span.Length;
            }

            sb.Append(formatter.Text(text.Slice(offset)));

            return sb.ToString();
        }
        */
        private Telegram.Bot.Types.MessageEntity[] Entities => _message.Entities;
    }
}
