using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telecord
{
    public class TelegramMessage
    {
        private readonly string _from;

        private string _text;
        private int _plainTextLen;
        private string _url;
        private bool _hidePreview;

        public TelegramMessage(string from)
        {
            _from = from;
        }

        public void SetText(string text, int plainTextLen)
        {
            _text = text;
            _plainTextLen = plainTextLen;
        }

        public void AppendUrl(string url)
        {
            if (_text.Length > 0 && !_text.EndsWith("\n"))
                _text += "\n";
            _text += url;
        }

        public void SetPhoto(string url)
        {
            _url = url;
        }

        public string Spoiler { get; set; }

        public void HidePreview()
        {
            _hidePreview = true;
        }

        public async Task SendAsync(TelegramBotClient telegram, ChatId chatId, ulong discordMessageId, CancellationToken ct)
        {
            IReplyMarkup reply = null;
            if (Spoiler != null)
                reply = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Показать спойлер", discordMessageId.ToString()));

            if (_url != null)
                await telegram.SendPhotoAsync(chatId, new InputOnlineFile(_url), GetText(), ParseMode.Html, replyMarkup: reply, cancellationToken: ct);
            else
                await telegram.SendTextMessageAsync(chatId, GetText(), ParseMode.Html, disableWebPagePreview: _hidePreview, replyMarkup: reply, cancellationToken: ct);
        }

        private string GetText()
        {
            var text = $"<b>{_from}</b>:";
            text += _text.Contains("\n") || _plainTextLen + _from.Length + 2 >= 60 ? "\n" : " ";
            return text + _text;
        }
    }
}
