using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;

namespace Maina.Administrative.Commands
{
    [Name("Administrative"), Group("settings"), Alias("setting", "set"), RequireUserPermission(GuildPermission.ManageChannels)]
    public class Settings : MainaBase
    {
        [Command("welcome channel")]
        public async Task SetJoinChannelAsync(SocketTextChannel channel)
        {
            Context.GuildConfig.WelcomeChannel = channel.Id;
            await ReplyAsync($"Set welcome channel to {channel.Mention}", updateGuild: true);
        }

        [Command("welcome message")]
        public async Task SetWelcomeMessageAsync([Remainder] string message)
        {
            Context.GuildConfig.WelcomeMessage = message;
            await ReplyAsync("Welcome message set.", updateGuild: true);
        }

        [Command("welcome message")]
        public async Task SetWelcomeMessageAsync()
        {
            Context.GuildConfig.WelcomeMessage = null;
            await ReplyAsync("Removed welcome message.", updateGuild: true);
        }
    }
}