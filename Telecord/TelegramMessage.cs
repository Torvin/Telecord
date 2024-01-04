using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telecord
{
    public class TelegramMessage
    {
        private readonly string _from;

        private string _text;
        private string _url;
        private bool _hidePreview;

        public TelegramMessage(string from)
        {
            _from = from;
        }

        public void SetText(string text, string plainText)
        {
            _text = text;
            PlainText = plainText;
        }

        public void AppendUrl(string url)
        {
            _text.TrimEnd();

            if (_text.Length > 0)
                _text += "\n\n";

            _text += url;
        }

        public void SetPhoto(string url)
        {
            _url = url;
        }

        public void HidePreview()
        {
            _hidePreview = true;
        }

        public async Task SendAsync(TelegramBotClient telegram, ChatId chatId, CancellationToken ct)
        {
            if (_url != null)
                await telegram.SendPhotoAsync(chatId, new InputFileUrl(_url), null, GetText(), ParseMode.Html, cancellationToken: ct);
            else
                await telegram.SendTextMessageAsync(chatId, GetText(), null, ParseMode.Html, disableWebPagePreview: _hidePreview, cancellationToken: ct);
        }

        public string GetText()
        {
            var text = $"<b>{_from}</b>:";
            text += _text.Contains("\n") || PlainText.Length + _from.Length + 2 >= 60 ? "\n" : " ";
            return text + _text;
        }

        public string PlainText { get; private set; }
    }
}
