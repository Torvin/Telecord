using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telecord.Options;

namespace Telecord
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TelegramUrlService>();
            services.AddHostedService<Bot>();

            services.AddOptions<Tokens>()
                .Bind(Configuration.GetSection("Tokens"))
                .PostConfigure(tokens =>
                {
                    if (tokens.Discord == null || tokens.Telegram == null)
                        throw new InvalidOperationException("Tokens were not provided.");
                });

            services.AddOptions<ChatOptions>()
                .Bind(Configuration.GetSection("ChatOptions"))
                .PostConfigure(options =>
                {
                    if (options.DiscordChannelId == 0 || options.TelegramChatId == 0)
                        throw new InvalidOperationException("Chat options were not provided.");
                });

            services.AddOptions<WebHostingOptions>()
                .Bind(Configuration.GetSection("WebHosting"))
                .PostConfigure(options =>
                {
                    if (options.RootUrl == null || options.SigningKey == null)
                        throw new InvalidOperationException("Web hosting options were not provided.");
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.Map("/file", app => app.UseMiddleware<TelegramFileMiddleware>());
        }
    }
}
