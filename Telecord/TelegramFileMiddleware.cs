using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Telecord.Options;
using Telegram.Bot;

namespace Telecord
{
    public class TelegramFileMiddleware
    {
        private static readonly Dictionary<string, string> ContentTypeByExtension = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".mp4"] = "video/mp4",
            [".oga"] = "audio/ogg",
        };

        private readonly RequestDelegate _next;
        private readonly Tokens _tokens;
        private readonly TelegramUrlService _urlService;
        private readonly ILogger<TelegramFileMiddleware> _logger;

        private readonly TelegramBotClient _telegram;
        private readonly HttpClient _httpClient = new HttpClient();

        public TelegramFileMiddleware(RequestDelegate next, IOptions<Tokens> tokens, TelegramUrlService urlService, ILogger<TelegramFileMiddleware> logger)
        {
            _next = next;
            _tokens = tokens.Value;
            _urlService = urlService;
            _logger = logger;

            _telegram = new TelegramBotClient(tokens.Value.Telegram);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var url = context.Request.GetEncodedUrl();
            var fileId = _urlService.GetFileId(url);
            if (fileId == null)
            {
                await _next(context);
                return;
            }

            var file = await _telegram.GetFile(fileId);
            using var response = await _httpClient.GetAsync($"https://api.telegram.org/file/bot{_tokens.Telegram}/{file.FilePath}");

            context.Response.StatusCode = (int)response.StatusCode;
            _logger.LogDebug($"Serving {file.FilePath}: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
                return;

            var query = QueryHelpers.ParseQuery(url);

            ContentTypeByExtension.TryGetValue(System.IO.Path.GetExtension(file.FilePath), out var contentType);
            context.Response.ContentType = GetParam(query, "type") ?? contentType ?? response.Content.Headers.ContentType.ToString();

            context.Response.ContentLength = response.Content.Headers.ContentLength;

            var name = GetParam(query, "name");
            if (name != null)
                context.Response.Headers.Append("Content-Disposition", $@"attachment; filename=""{name}""");

            using var content = await response.Content.ReadAsStreamAsync();

            await content.CopyToAsync(context.Response.Body);
        }

        private static string GetParam(IDictionary<string, StringValues> query, string name)
        {
            if (!query.TryGetValue(name, out var values) || values.Count != 1)
                return null;
            return values[0];
        }
    }
}
