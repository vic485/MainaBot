﻿using System;
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
using System.Collections.Generic;
using Maina.Configuration;
using Raven.Client.Documents;

namespace Maina
{
    internal static class Program
    {
        private static LocalSettings _settings;
        
        private static async Task Main(string[] args)
        {
            _settings = SettingsLoader.Load();
            Logger.Initialize(LogType.Debug, Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), "0.0.1");

            using (var services = SetupServices())
            {
				CheckArguments (args, services);                
                services.GetRequiredService<DatabaseManager>().CheckConfig();
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

		private static void CheckArguments(string[] args, ServiceProvider services)
		{
			List<string> argsList = new List<string>(args);
			if (argsList.Contains("-r") || argsList.Contains("--reset"))
				services.GetRequiredService<DatabaseManager>().ResetConfig();
			if (argsList.Contains("--update-DB"))
				services.GetRequiredService<DatabaseManager>().UpdateGuilds();
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
                .AddSingleton(new DocumentStore
                {
                    Certificate = _settings.Certificate,
                    Database = _settings.DatabaseName,
                    Urls = _settings.DatabaseUrls
                }.Initialize())
                .AddSingleton<DatabaseManager>()
                .AddSingleton<DiscordHandler>()
                .AddSingleton<HTTPServerManager>()
                .AddSingleton<RSSManager>()
                .BuildServiceProvider();
    }
}
