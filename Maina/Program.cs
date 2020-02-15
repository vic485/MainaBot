using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Maina
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Logger.Initialize(LogType.Warning, Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), "0.0.1");

            using (var services = SetupServices())
            {
                services.GetRequiredService<DatabaseManager>().CheckConfig();
                await services.GetRequiredService<DiscordHandler>().InitializeAsync(services).ConfigureAwait(false);

                // Keep bot alive
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
                .BuildServiceProvider();
    }
}