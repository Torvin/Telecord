using System;

namespace Telecord.Options
{
    public class WebHostingOptions
    {
        public Uri RootUrl { get; set; }
        public string SigningKey { get; set; }
    }
}
