using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telecord.Options;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telecord
{
    class Bot : BackgroundService
    {
        private const int DiscordMessageLengthLimit = 2000;

        private readonly Tokens _tokens;
        private readonly ChatOptions _chatOptions;
        private readonly ILogger<Bot> _logger;
        private readonly TelegramUrlService _urlService;

        private readonly TelegramBotClient _telegram;
        private readonly DiscordSocketClient _discord = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
        private readonly Func<Task> EnsureDiscordConnected;
        private readonly TelegramMessageConverter _telegramConverter;

        private string _telegramBotName;

        public Bot(IOptions<Tokens> tokens, IOptions<ChatOptions> chatOptions, ILogger<Bot> logger, TelegramUrlService urlService)
        {
            _tokens = tokens.Value;
            _chatOptions = chatOptions.Value;
            _logger = logger;
            _urlService = urlService;

            _telegram = new TelegramBotClient(_tokens.Telegram);

            var discordConnected = new TaskCompletionSource<object>();
            EnsureDiscordConnected = () => discordConnected.Task;
            _discord.Ready += async () => discordConnected.TrySetResult(null);

            _telegramConverter = new TelegramMessageConverter(DiscordMessageLengthLimit, GetFileUrl);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _telegram.OnMessage += (s, e) => OnTelegram(e, ct);
            _telegram.OnCallbackQuery += (s, e) => OnTelegramCallback(e, ct);
            _discord.MessageReceived += m => OnDiscord(m, ct);

            _logger.LogDebug("Connecting");

            var getMe = _telegram.GetMeAsync();

            await _discord.LoginAsync(TokenType.Bot, _tokens.Discord);
            await _discord.StartAsync();
            await EnsureDiscordConnected();

            _telegramBotName = (await getMe).Username;
            _telegram.StartReceiving(null, ct);

            try
            {
                _logger.LogDebug("Connected");
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (TaskCanceledException)
            {
                _telegram.StopReceiving();
                await _discord.StopAsync();
                _logger.LogDebug("Disconnected");
                throw;
            }
        }

        private async void OnTelegram(MessageEventArgs e, CancellationToken ct)
        {
            try
            {
                if (e.Message.Chat.Type == ChatType.Private)
                {
                    await OnTelegramDm(e.Message, ct);
                    return;
                }

                if (e.Message.Chat.Id != _chatOptions.TelegramChatId) return; // ignore unknown chats
                _logger.LogDebug($"sending #{e.Message.MessageId} to discord from {e.Message.From.Username}");

                var (parts, embed) = _telegramConverter.Convert(e.Message);

                foreach (var part in parts)
                {
                    await GetDiscordChannel().SendMessageAsync(part, embed: embed);
                    embed = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending #{e.Message.MessageId}");

                if (_chatOptions.SendErrorsTo != null)
                {
                    await _telegram.SendTextMessageAsync(_chatOptions.SendErrorsTo,
                        $"Id: {e.Message.MessageId}\n<pre>{TelegramUtils.Escape(ex.ToString())}</pre>",
                        ParseMode.Html, cancellationToken: ct);
                    await _telegram.ForwardMessageAsync(_chatOptions.SendErrorsTo, _chatOptions.TelegramChatId, e.Message.MessageId, false, ct);
                }
            }
        }

        private async void OnTelegramCallback(CallbackQueryEventArgs e, CancellationToken ct)
        {
            try
            {
                var spoilerId = e.CallbackQuery.Data;
                var (dmsg, text) = await ReadSpoiler(spoilerId);

                if (text.Length > 200)
                {
                    try
                    {
                        _logger.LogDebug($"sending spoiler {spoilerId} to {e.CallbackQuery.From.Username} via DM");
                        await SendSpoilerAsDm(e.CallbackQuery.From.Id, dmsg, text, ct);
                    }
                    catch (ChatNotInitiatedException)
                    {
                        _logger.LogDebug($"user {e.CallbackQuery.From.Username} hasn't started the conversation yet, deeplinking instead");
                    }
                    await _telegram.AnswerCallbackQueryAsync(e.CallbackQuery.Id, url: $"t.me/{_telegramBotName}?start={spoilerId}", cancellationToken: ct);
                }
                else
                {
                    _logger.LogDebug($"showing spoiler {spoilerId} to {e.CallbackQuery.From.Username}");
                    await _telegram.AnswerCallbackQueryAsync(e.CallbackQuery.Id, text, true, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing spoiler for #{e.CallbackQuery.Data}");

                if (_chatOptions.SendErrorsTo != null)
                {
                    await _telegram.SendTextMessageAsync(_chatOptions.SendErrorsTo,
                        $"Id: {e.CallbackQuery.Data}\n<pre>{TelegramUtils.Escape(ex.ToString())}</pre>",
                        ParseMode.Html, cancellationToken: ct);
                }
            }
        }

        private async Task OnTelegramDm(Message msg, CancellationToken ct)
        {
            _logger.LogDebug($"`{msg.Text}` from {msg.From.Username}");

            if (!TryParseStartSpoiler(msg.Text, out var id)) { return; }
            _logger.LogDebug($"showing spoiler {id} to {msg.From.Username} from /start");

            var (dmsg, text) = await ReadSpoiler(id);
            await SendSpoilerAsDm(msg.Chat.Id, dmsg, text, ct);
        }

        private static readonly Regex ReadSpoilerRegex = new Regex(@"^/start (\d+)$");
        public static bool TryParseStartSpoiler(string startPayload, out string result)
        {
            var match = ReadSpoilerRegex.Match(startPayload);
            if (!match.Success)
            {
                result = null;
                return false;
            }

            result = match.Groups[1].Value;
            return true;
        }

        private async Task SendSpoilerAsDm(ChatId chatId, IMessage dmsg, string text, CancellationToken ct)
        {
            await _telegram.SendTextMessageAsync(chatId, $"<b>{dmsg.Author.Username}</b>:\n{text}", ParseMode.Html, cancellationToken: ct);
        }

        private async Task<(IMessage, string)> ReadSpoiler(string id)
        {
            var msg = await GetDiscordChannel().GetMessageAsync(ulong.Parse(id));
            return (msg, new DiscordMessageReader(msg).ReadSpoiler(_discord));
        }

        private ITextChannel GetDiscordChannel()
        {
            return (ITextChannel)_discord.GetChannel(_chatOptions.DiscordChannelId);
        }

        private async Task OnDiscord(SocketMessage msg, CancellationToken ct)
        {
            try
            {
                if (msg.Channel.Id != _chatOptions.DiscordChannelId) return;
                if (msg.Author.Id == _discord.CurrentUser.Id) return;

                _logger.LogDebug($"sending #{msg.Id} to telegram from {msg.Author.Username}");

                await new DiscordMessageReader(msg).Read(_discord).SendAsync(_telegram, _chatOptions.TelegramChatId, msg.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending #{msg.Id} from {msg.Author.Username}");

                if (_chatOptions.SendErrorsTo != null)
                {
                    await _telegram.SendTextMessageAsync(_chatOptions.SendErrorsTo,
                        $"Id: {msg.Id}\nFrom: {msg.Author.Username}\n<pre>{TelegramUtils.Escape(msg.Content)}</pre>\n\n<pre>{TelegramUtils.Escape(ex.ToString())}</pre>",
                        ParseMode.Html, cancellationToken: ct);
                }
            }
        }

        private string GetFileUrl(string fileId, string extension = null, string mimeType = null, string fileName = null)
            => _urlService.CreateUrl(fileId, extension, mimeType, fileName).OriginalString;
    }
}
