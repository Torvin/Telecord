using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telecord.Options
{
    class ChatOptions
    {
        public ulong DiscordChannelId { get; set; }
        public long TelegramChatId { get; set; }
        public int? SendErrorsTo { get; set; }
    }
}
