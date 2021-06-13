using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Xunit;

namespace Telecord.Tests
{
    public class DiscordMessageReaderTests
    {
        private static Expectation<string> TestSpoiler(string markdown)
        {
            var spoiler = new DiscordMessageReader(new MockMessage
            {
                Author = new MockUser { Username = "xx" },
                Content = markdown,
            }).ReadSpoiler(null);

            return new Expectation<string>(expected => Assert.Equal(expected, spoiler));
        }

        [Fact]
        public void SpoilerWithQuote()
        {
            TestSpoiler("> В мусульманских группах в социальных сетях появились призывы найти и наказать Илью Мэддисона, в том числе от сторонников ИГИЛ[33].\n||Наконец-то ИГИЛ сделал что-то хорошее.||")
                .ToBe("В мусульманских группах в социальных сетях появились призывы найти и наказать Илью Мэддисона, в том числе от сторонников ИГИЛ[33].\nНаконец-то ИГИЛ сделал что-то хорошее.");
        }

        [Fact]
        public void LinkWithQuotes()
        {
            var message = new DiscordMessageReader(new MockMessage
            {
                Author = new MockUser { Username = "xx" },
                Content = "Ссылка: <https://en.wikipedia.org/wiki/Special:Search/suicide_incategory:\"Transgender_and_transsexual_men\">",
            }).Read(null);

            Assert.Equal("<b>xx</b>:\nСсылка: <a href=\"https://en.wikipedia.org/wiki/Special:Search/suicide_incategory:%22Transgender_and_transsexual_men%22\">https://en.wikipedia.org/wiki/Special:Search/suicide_incategory:\"Transgender_and_transsexual_men\"</a>", message.GetText());
        }
    }

    class MockMessage : IMessage
    {
        public MessageType Type { get; set; }

        public MessageSource Source { get; set; }

        public bool IsTTS { get; set; }

        public bool IsPinned { get; set; }

        public string Content { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? EditedTimestamp { get; set; }

        public IMessageChannel Channel { get; set; }

        public IUser Author { get; set; }

        public IReadOnlyCollection<IAttachment> Attachments { get; set; } = new IAttachment[0];

        public IReadOnlyCollection<IEmbed> Embeds { get; set; }

        public IReadOnlyCollection<ITag> Tags { get; set; }

        public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; }

        public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; }

        public IReadOnlyCollection<ulong> MentionedUserIds { get; set; }

        public MessageActivity Activity { get; set; }

        public MessageApplication Application { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong Id { get; set; }

        public Task DeleteAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }

    class MockUser : IUser
    {
        public string AvatarId { get; set; }

        public string Discriminator { get; set; }

        public ushort DiscriminatorValue { get; set; }

        public bool IsBot { get; set; }

        public bool IsWebhook { get; set; }

        public string Username { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong Id { get; set; }

        public string Mention { get; set; }

        public IActivity Activity { get; set; }

        public UserStatus Status { get; set; }

        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAvatarUrl()
        {
            throw new NotImplementedException();
        }

        public Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}
