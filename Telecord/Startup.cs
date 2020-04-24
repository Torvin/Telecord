using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddControllers();

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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
