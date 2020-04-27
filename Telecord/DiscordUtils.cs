using System.Text.RegularExpressions;

namespace Telecord
{
    public static class DiscordUtils
    {
        private static readonly Regex EscapeRegex = new Regex(@"([^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}.,;!?\s\u00FF-\uFFFF])");

        public static string Escape(string text)
        {
            return EscapeRegex.Replace(text.ToString(), @"\$1");
        }
    }
}
