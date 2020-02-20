using System;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Database;
using Maina.Database.Models;
using Maina.HTTP;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Maina.Core
{
    public class BotContext : SocketCommandContext
    {
        public IDocumentSession Session { get; }
        public DatabaseManager Database { get; }
        public BotConfig Config { get; }
        public GuildConfig GuildConfig { get; }
        public HTTPServerManager HttpServerManager { get; }

        public BotContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider provider) : base(client,
            msg)
        {
            Database = provider.GetRequiredService<DatabaseManager>();
            Session = Database.Store.OpenSession();
            Config = Database.Get<BotConfig>("Config");
            GuildConfig = Database.Get<GuildConfig>($"guild-{Guild.Id}");
            HttpServerManager = provider.GetRequiredService<HTTPServerManager>();
        }
    }
}