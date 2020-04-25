using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telecord
{
    public class TelegramMessageReader
    {
        public delegate string GetFileUrl(string fileId, string mimeType = null, string fileName = null);

        private static readonly Dictionary<MessageEntityType, string> Tags = new Dictionary<MessageEntityType, string>
        {
            [MessageEntityType.Bold] = "**",
            [MessageEntityType.Italic] = "*",
            [MessageEntityType.Underline] = "__",
            [MessageEntityType.Strikethrough] = "~~",
            [MessageEntityType.Code] = "``",
            [MessageEntityType.Pre] = "```",

            [MessageEntityType.Mention] = null,
            [MessageEntityType.BotCommand] = null,
            [MessageEntityType.Cashtag] = null,
            [MessageEntityType.Email] = null,
            [MessageEntityType.Hashtag] = null,
            [MessageEntityType.PhoneNumber] = null,
            [MessageEntityType.Url] = null,
        };

        private static readonly MessageEntityType[] NoSpaceLifting = { MessageEntityType.Pre, MessageEntityType.Code };
        private static readonly MessageEntityType[] NoEscape = { MessageEntityType.Url };
        private static readonly MessageEntityType[] EscapeBacktick = { MessageEntityType.Pre, MessageEntityType.Code };

        private static readonly Regex EscapeRegex = new Regex(@"([^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}.,;!?\s])");
        private static readonly Regex EscapeBacktickRegex = new Regex(@"`(?=`)|^`|`$");

        private readonly Message _message;

        public TelegramMessageReader(Message message)
        {
            _message = message;
        }

        public (string text, Embed embed) Read(GetFileUrl getFileUrl)
        {
            var text = ReadText() ?? "";

            Embed embed = null;

            if (_message.Photo != null && _message.Photo.Length > 0)
            {
                var photos = _message.Photo.OrderByDescending(p => p.Width).ToArray();
                var photo = photos.FirstOrDefault(p => p.Width == 800) ?? photos[0];

                embed = new EmbedBuilder()
                    .WithImageUrl(getFileUrl(photo.FileId))
                    .Build();
            }
            else if (_message.Sticker != null)
            {
                if (_message.Sticker.IsAnimated)
                {
                    text += @"\<animated sticker\>";
                }
                else
                {
                    embed = new EmbedBuilder()
                        .WithImageUrl(getFileUrl(_message.Sticker.FileId))
                        .Build();
                }
            }
            else if (_message.Animation != null)
            {
                text += "GIF: " + getFileUrl(_message.Animation.FileId);
            }
            else if (_message.Voice != null)
            {
                text += "Audio: " + getFileUrl(_message.Voice.FileId);
            }
            else if (_message.VideoNote != null)
            {
                text += "Video: " + getFileUrl(_message.VideoNote.FileId);
            }
            else if (_message.Document != null)
            {
                var doc = _message.Document;
                text += "File " + EscapeDiscord(doc.FileName) + " " + getFileUrl(doc.FileId, doc.MimeType, doc.FileName);
            }

            var message = $"**{EscapeDiscord(_message.From.Username)}**:";
            message += text.Contains('\n') || text.Length > 50 ? "\n" : " ";
            message += text;

            return (message, embed);
        }

        private static string EscapeDiscord(ReadOnlySpan<char> str)
        {
            return EscapeRegex.Replace(str.ToString(), @"\$1");
        }


        public string ReadText()
        {
            var stack = new Stack<MessageEntity>();
            var writer = new MessageWriter();

            var entities = (_message.Entities ?? new MessageEntity[0])
                .OrderBy(e => e.Offset)
                .ThenByDescending(e => e.Offset + e.Length)
                .ThenBy(e => Array.IndexOf(_message.Entities, e));

            var offset = 0;
            var text = _message.Text.AsSpan();

            foreach (var entity in entities)
            {
                while (stack.Count > 0 && GetFinish(stack.Peek()) <= entity.Offset)
                    Pop(text);

                if (offset < entity.Offset)
                {
                    Text(text[offset..entity.Offset]);
                    offset = entity.Offset;
                }

                stack.Push(entity);
                Start(entity);
            }

            while (stack.Count > 0)
                Pop(text);

            Text(text.Slice(offset));

            return writer.Finish();

            void Pop(ReadOnlySpan<char> text)
            {
                var entity = stack.Peek();

                if (offset < GetFinish(entity))
                {
                    Text(text[offset..GetFinish(entity)]);
                    offset = GetFinish(entity);
                }

                stack.Pop();
                End(entity);
            }

            void Text(ReadOnlySpan<char> text)
            {
                if (stack.Count > 0 && NoSpaceLifting.Contains(stack.Peek().Type))
                {
                    TextOnly(text);
                    return;
                }

                // lift leading spaces
                var index = FindFirstNonSpace(text);
                if (index is int i1 && i1 != text.Length - 1)
                {
                    Restart(text[..i1]);

                    // trim leading spaces
                    text = text.Slice(i1);
                }

                // trim trailing spaces
                index = FindLastNonSpace(text);
                var str = index != null ? text[..index.Value] : text;
                TextOnly(str);

                // lift trailing spaces
                if (index != null)
                    Restart(text.Slice(index.Value));
            }

            void TextOnly(ReadOnlySpan<char> str)
            {
                if (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (NoEscape.Contains(top.Type))
                    {
                        writer.Text(str);
                        return;
                    }
                    else if (EscapeBacktick.Contains(top.Type))
                    {
                        writer.Text(EscapeBacktickRegex.Replace(str.ToString(), "`\u200b"));
                        return;
                    }
                }

                writer.Text(EscapeDiscord(str));
            }

            static bool Tag(MessageEntityType type, Action<string> append)
            {
                if (!Tags.TryGetValue(type, out var tag))
                    return false;

                if (tag != null)
                    append(tag);

                return true;
            }

            void Start(MessageEntity entity, bool final = true)
            {
                if (Tag(entity.Type, writer.Start))
                    return;

                switch (entity.Type)
                {
                    case MessageEntityType.TextLink:
                        return;

                    case MessageEntityType.TextMention:
                        if (final) writer.Start("\\@");
                        return;
                }

                throw new ArgumentOutOfRangeException($"Unknown entity type: {entity.Type}");
            }

            void End(MessageEntity entity, bool final = true)
            {
                if (Tag(entity.Type, writer.End))
                    return;

                switch (entity.Type)
                {
                    case MessageEntityType.TextLink:
                        if (final) writer.Text($" ({entity.Url})");
                        return;

                    case MessageEntityType.TextMention:
                        if (final) writer.End("");
                        return;

                }

                throw new ArgumentOutOfRangeException($"Unknown entity type: {entity.Type}");
            }

            void Restart(ReadOnlySpan<char> toOutput)
            {
                if (toOutput.Length == 0) return;

                for (var i = 0; i < toOutput.Length; i++)
                {
                    if (toOutput[i] != ' ')
                        throw new ArgumentException("Expected spaces only");
                }

                foreach (var item in stack)
                    End(item, false);

                TextOnly(toOutput);

                foreach (var item in stack.Reverse())
                    Start(item, false);
            }
        }

        class MessageWriter
        {
            private readonly StringBuilder _builder = new StringBuilder();
            private readonly Stack<string> _stack = new Stack<string>();

            public MessageWriter()
            {
                _stack.Push(null);
            }

            public void Start(string opening)
            {
                _stack.Push(opening);
            }

            public void Text(ReadOnlySpan<char> text)
            {
                if (text.Length == 0) return;

                var unpushed = _stack.TakeWhile(s => s != null);
                foreach (var item in unpushed)
                    _builder.Append(item);

                if (_stack.Peek() != null)
                    _stack.Push(null);

                _builder.Append(text);
            }

            public void End(string closing)
            {
                if (_stack.Count == 1)
                    throw new InvalidOperationException("Unmatched end: " + closing);

                var item = _stack.Pop();
                if (item == null)
                {
                    _stack.Pop();
                    if (_stack.Peek() != null)
                        _stack.Push(null);
                    _builder.Append(closing);
                }
            }

            public string Finish()
            {
                if (_stack.Count > 1)
                    throw new InvalidOperationException("Stack not empty: " + string.Join(", ", _stack.Select(item => $"`{item}`")));

                return _builder.ToString();
            }
        }

        private int GetFinish(MessageEntity entity)
        {
            return entity.Offset + entity.Length;
        }

        private static int? FindFirstNonSpace(ReadOnlySpan<char> text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                if (i != 0)
                {
                    return i;
                }
                return null;
            }

            return text.Length;
        }

        private static int? FindLastNonSpace(ReadOnlySpan<char> text)
        {
            for (var i = text.Length; i > 0; i--)
            {
                if (text[i - 1] == ' ') continue;
                if (i != text.Length)
                {
                    return i;
                }
                break;
            }

            return null;
        }
    }
}
