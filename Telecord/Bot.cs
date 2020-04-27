using System;
using System.Collections.Generic;
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
        private readonly DiscordSocketClient _discord = new DiscordSocketClient();
        private readonly Func<Task> EnsureDiscordConnected;
        private readonly TelegramMessageConverter _telegramConverter;

        public Bot(IOptions<Tokens> tokens, IOptions<ChatOptions> chatOptions, ILogger<Bot> logger, TelegramUrlService urlService)
        {
            _tokens = tokens.Value;
            _chatOptions = chatOptions.Value;
            _logger = logger;
            _urlService = urlService;

            _telegram = new TelegramBotClient(_tokens.Telegram);

            var discordConnected = new TaskCompletionSource<object>();
            EnsureDiscordConnected = () => discordConnected.Task;
            _discord.Connected += async () => discordConnected.TrySetResult(null);

            _telegramConverter = new TelegramMessageConverter(DiscordMessageLengthLimit, GetFileUrl);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _telegram.OnMessage += (s, e) => OnTelegram(e, ct);
            _telegram.OnCallbackQuery += (s, e) => OnTelegramCallback(e, ct);
            _discord.MessageReceived += m => OnDiscord(m, ct);

            _logger.LogDebug("Connecting");

            await _discord.LoginAsync(TokenType.Bot, _tokens.Discord);
            await _discord.StartAsync();
            await EnsureDiscordConnected();

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
                if (e.Message.Chat.Id != _chatOptions.TelegramChatId) return; // ignore DMs for now
                _logger.LogDebug($"sending #{e.Message.MessageId} to discord from {e.Message.From.Username}");

                var (parts, embed) = _telegramConverter.Convert(e.Message);

                for (var i = 0; i < parts.Length; i++)
                    await GetDiscordChannel().SendMessageAsync(parts[i], embed: i == 0 ? embed : null);
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
            _logger.LogDebug($"showing spoiler to {e.CallbackQuery.From.Username}");
            await _telegram.AnswerCallbackQueryAsync(e.CallbackQuery.Id, e.CallbackQuery.Data, true, cancellationToken: ct);
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

                await new DiscordMessageReader(msg).Read(_discord).SendAsync(_telegram, _chatOptions.TelegramChatId, ct);
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
