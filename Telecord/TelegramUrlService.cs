using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telecord.Options;

namespace Telecord
{
    public class TelegramUrlService : IDisposable
    {
        private readonly WebHostingOptions _options;
        private readonly HMACSHA256 _hmac;
        private readonly ILogger<TelegramUrlService> _logger;

        public TelegramUrlService(IOptions<WebHostingOptions> options, ILogger<TelegramUrlService> logger)
        {
            _options = options.Value;
            _hmac = new HMACSHA256(Convert.FromBase64String(_options.SigningKey));
            _logger = logger;
        }

        public Uri CreateUrl(string fileId, string mimeType = null, string fileName = null)
        {
            var signature = GetSignature(fileId);
            var path = $"file/{Uri.EscapeDataString(fileId)}?hash={Uri.EscapeDataString(signature)}";

            if (mimeType != null)
                path = QueryHelpers.AddQueryString(path, "type", mimeType);
            if (fileName != null)
                path = QueryHelpers.AddQueryString(path, "name", fileName);

            return new Uri(_options.RootUrl, path);
        }

        public string GetFileId(string url)
        {
            try
            {
                var uri = new Uri(url, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                    uri = new Uri(new Uri("http://example.com/"), uri);

                var query = QueryHelpers.ParseQuery(uri.Query);
                if (!query.TryGetValue("hash", out var hashValue) || hashValue.Count != 1)
                    return null;

                var hash = hashValue[0];
                var fileId = uri.Segments[^1];

                return GetSignature(fileId) == hash ? fileId : null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Signature verification failed: " + ex);
                return null;
            }
        }

        private string GetSignature(string fileId)
        {
            var data = Encoding.UTF8.GetBytes(fileId);
            return Convert.ToBase64String(_hmac.ComputeHash(data));
        }

        void IDisposable.Dispose()
        {
            _hmac.Dispose();
        }
    }
}
