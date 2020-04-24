using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telecord.Options;

namespace Telecord
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(ConfigureHostConfiguration)
                .ConfigureServices(ConfigureServices);
        }

        private static void ConfigureHostConfiguration(HostBuilderContext hostContext, IConfigurationBuilder builder)
        {
            builder.AddJsonFile("appsettings.Private.json", optional: true, reloadOnChange: true);
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            var configuration = hostContext.Configuration;

            services.AddHostedService<Bot>();

            services.AddOptions<Tokens>()
                .Bind(configuration.GetSection("Tokens"))
                .PostConfigure(tokens =>
                {
                    if (tokens.Discord == null || tokens.Telegram == null)
                        throw new InvalidOperationException("Tokens were not provided.");
                });

            services.AddOptions<ChatOptions>()
                .Bind(configuration.GetSection("ChatOptions"))
                .PostConfigure(options =>
                {
                    if (options.DiscordChannelId == 0 || options.TelegramChatId == 0)
                        throw new InvalidOperationException("Chat options were not provided.");
                });
        }
    }
}
