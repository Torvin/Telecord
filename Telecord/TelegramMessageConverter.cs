using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telecord
{
    public class TelegramMessageConverter
    {
        public delegate string GetFileUrl(string fileId, string extension = null, string mimeType = null, string fileName = null);

        private readonly int _maxPartLength;
        private readonly GetFileUrl _getFileUrl;

        public TelegramMessageConverter(int maxPartLength, GetFileUrl getFileUrl)
        {
            _maxPartLength = maxPartLength;
            _getFileUrl = getFileUrl;
        }

        public (string[] text, Embed embed) Convert(Message message)
        {
            var from = $"**{DiscordUtils.Escape(message.From.Username)}**:";

            if (message.ForwardFromChat != null)
            {
                from += $" https://t.me/{message.ForwardFromChat.Username}/{message.ForwardFromMessageId}";
                return (new[] { from }, null);
            }

            var quoteReply = QuoteReply(message.ReplyToMessage);
            var forward = message.ForwardFrom != null ? $"**Forwarded from @{DiscordUtils.Escape(message.ForwardFrom.Username)}**"
                : message.ForwardSenderName != null ? $"**Forwarded from {DiscordUtils.Escape(message.ForwardSenderName)}**"
                : null;

            string link = null;
            Embed embed = null;

            if (message.Photo != null && message.Photo.Length > 0)
            {
                var photos = message.Photo.OrderByDescending(p => p.Width).ToArray();
                var photo = photos.FirstOrDefault(p => p.Width == 800) ?? photos[0];
                var url = _getFileUrl(photo.FileId);

                embed = new EmbedBuilder()
                    .WithImageUrl(url)
                    .WithDescription($"[photo]({url})")
                    .Build();
            }
            else if (message.Sticker != null)
            {
                if (message.Sticker.IsAnimated)
                {
                    link = @"\<animated sticker\>";
                }
                else
                {
                    var url = _getFileUrl(message.Sticker.FileId);

                    embed = new EmbedBuilder()
                        .WithImageUrl(url)
                        .WithDescription($"[sticker]({url})")
                        .Build();
                }
            }
            else if (message.Animation != null)
            {
                link = "GIF: " + _getFileUrl(message.Animation.FileId, ".mp4");
            }
            else if (message.Voice != null)
            {
                link = "Audio: " + _getFileUrl(message.Voice.FileId);
            }
            else if (message.VideoNote != null)
            {
                link = "Video: " + _getFileUrl(message.VideoNote.FileId, ".mp4");
            }
            else if (message.Document != null)
            {
                var doc = message.Document;
                link = "File " + DiscordUtils.Escape(doc.FileName) + " " + _getFileUrl(doc.FileId, null, doc.MimeType, doc.FileName);
            }

            if (forward != null)
                from += "\n" + forward;

            var parts = new List<string>();
            var (text, entities) = message.GetTextAndEntities();
            var start = 0;

            for (var i = 0; ; i++)
            {
                var length = _maxPartLength;
                length -= from.Length + 1; // +1 for space or \n

                if (i == 0 && quoteReply != null)
                    length -= quoteReply.Length;

                var (part, newStart) = TelegramMessageSlicer.GetNextSlice(text, entities, start, length);
                if (part == null && i > 0)
                    break;

                if (i == 0 && quoteReply != null)
                    part = quoteReply + part;

                if (i == 0 && part == null)
                    break;

                part = from + (forward != null || (part != null && part.Contains('\n')) ? '\n' : ' ') + part;

                parts.Add(part);
                start = newStart;
            }

            if (link != null)
            {
                if (parts.Count == 0)
                {
                    parts.Add(from + " " + link);
                }
                else
                {
                    var lastPart = parts[parts.Count - 1];
                    if (lastPart.Length + link.Length + 1 <= _maxPartLength) // +1 for \n
                        parts[parts.Count - 1] = lastPart + (lastPart != "" ? "\n" : "") + link;
                    else
                        parts.Add(from + " " + link);
                }
            }

            return (parts.ToArray(), embed);
        }

        private string QuoteReply(Message message)
        {
            if (message == null) return null;
            var (text, entities) = message.GetTextAndEntities();
            if (text == null) return null;

            var start = 0;

            // skip username
            if (message.From.IsBot && entities.FirstOrDefault()?.Type == MessageEntityType.Bold)
                start = entities[0].GetFinish() + 2; // 2 for ": "

            // skip pre
            start = entities
                .SkipWhile(e => e.GetFinish() < start)
                .TakeWhile(e => e.Type == MessageEntityType.Pre)
                .Select(e => (int?)e.GetFinish())
                .OrderByDescending(x => x)
                .FirstOrDefault() ?? start;

            // skip leading enters
            while (text[start] == '\n')
                start++;

            // take first line
            var end = text.IndexOf('\n', start);
            if (end == -1)
                end = text.Length;

            if (end - start > 50)
            {
                end = start + 50;
                text = text[..(end - 3)] + "...";
            }

            // trim entities
            entities = entities
                .Where(e => e.Offset < end && e.Offset >= start)
                .Select(e => e.GetFinish() < end ? e : new MessageEntity { Type = e.Type, Offset = e.Offset - start, Length = end - e.Offset, Url = e.Url, User = e.User })
                .ToArray();

            return "> " + TelegramMessageReader.Read(text.AsSpan()[start..end], entities, true) + "\n";
        }
    }
}
