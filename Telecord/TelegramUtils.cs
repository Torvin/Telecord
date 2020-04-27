namespace Telecord
{
    public static class TelegramUtils
    {
        public static string Escape(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
