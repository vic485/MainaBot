using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;

namespace Maina.Administrative.Commands
{
    public class Settings : MainaBase
    {
        [Command("set welcome channel")]
        public async Task SetJoinChannelAsync(SocketTextChannel channel)
        {
            Context.GuildConfig.WelcomeChannel = channel.Id;
            await ReplyAsync($"Set welcome channel to {channel.Mention}");
        }

        [Command("set welcome message")]
        public async Task SetWelcomeMessageAsync([Remainder] string message)
        {
            Context.GuildConfig.WelcomeMessage = message;
            await ReplyAsync("Welcome message set.");
        }

        [Command("set welcome message")]
        public async Task SetWelcomeMessageAsync()
        {
            Context.GuildConfig.WelcomeMessage = null;
            await ReplyAsync("Removed welcome message.");
        }
    }
}