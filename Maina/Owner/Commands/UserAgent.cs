using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Maina.Core;
using Maina.HTTP;
using Maina.HTTP.Server;

namespace Maina.Owner.Commands
{
    [RequireOwner, Group("useragent")]
    public class UserAgent : MainaBase
    {
        [Command("add")]
        public async Task AddUserAgent(string agent)
        {
            if (Context.Config.UserAgents.Contains(agent))
            {
                await ReplyAsync("This user agent has already been added.");
                return;
            }

            Context.Config.UserAgents.Add(agent);
            await ReplyAsync($"Added user agent `{agent}`.", updateConfig: true);
            Context.HttpServerManager.ChangeAgents(Context.Config.UserAgents);
        }

        [Command("remove")]
        public async Task RemoveUserAgent(string agent)
        {
            if (!Context.Config.UserAgents.Contains(agent))
            {
                await ReplyAsync("This user agent has not been added.");
                return;
            }

            Context.Config.UserAgents.Remove(agent);
            await ReplyAsync($"Removed user agent `{agent}`. Restarting HTTP server...", updateConfig: true);
            Context.HttpServerManager.ChangeAgents(Context.Config.UserAgents);
        }

        [Command("list")]
        public async Task ListUserAgent()
        {
            if (Context.Config.UserAgents.Count == 0)
            {
                await ReplyAsync("No user agents have been added");
                return;
            }

            var embed = CreateEmbed(EmbedColor.Purple)
                .WithTitle("HTTP User Agents")
                .WithDescription(Context.Config.UserAgents.Aggregate("", (current, agent) => current + $"{agent}\n"))
                .Build();

            await ReplyAsync(string.Empty, embed);
        }
    }
}