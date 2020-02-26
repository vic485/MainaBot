using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;
using Maina.RSS;
using Maina.HTTP;
using Microsoft.Extensions.DependencyInjection;

namespace Maina
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Logger.Initialize(LogType.Debug, Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), "0.0.1");

            using (var services = SetupServices())
            {
                if (args.Length == 1 && (args[0] == "-r" || args[0] == "--reset"))
                    services.GetRequiredService<DatabaseManager>().ResetConfig();
                services.GetRequiredService<DatabaseManager>().CheckConfig();
				services.GetRequiredService<DatabaseManager>().UpdateGuilds();
                await services.GetRequiredService<DiscordHandler>().InitializeAsync(services).ConfigureAwait(false);

                var trusted = services.GetRequiredService<DatabaseManager>().Get<BotConfig>("Config").UserAgents
                    .ToArray();
                services.GetService<HTTPServerManager>().Initialize(null, trusted);
                services.GetService<RSSManager>().Initialize();


                /* TODO
                 * Here you would put a command line program to manage the bot.
                 * Mainly so it can be shutdown safely (close/Dispose all the services).
                 * (IDisposable objects may not necessarilly be Disposed on program exit). */
                await Task.Delay(-1);
            }
        }

        private static ServiceProvider SetupServices()
            => new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 20,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Error
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    ThrowOnError = true,
                    IgnoreExtraArgs = false,
                    CaseSensitiveCommands = false,
                    DefaultRunMode = RunMode.Async
                }))
                .AddSingleton<DatabaseManager>()
                .AddSingleton<DiscordHandler>()
                .AddSingleton<HTTPServerManager>()
                .AddSingleton<RSSManager>()
                .BuildServiceProvider();
    }
}
