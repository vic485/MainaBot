using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database;
using Maina.WebHooks;
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
                services.GetRequiredService<DatabaseManager>().CheckConfig();
                await services.GetRequiredService<DiscordHandler>().InitializeAsync(services).ConfigureAwait(false);

				string [] trusted = { "Kuuki-Scans" };
				services.GetService<WebHooksManager>().Initialize(null, trusted);


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
				.AddSingleton<WebHooksManager>()
                .BuildServiceProvider();
    }
}
