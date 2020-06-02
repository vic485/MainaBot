using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Maina.Core;

namespace Maina.General.Commands
{
    [Name("General")]
    public class HelpCommand : MainaBase
    {
        private CommandService _commandService;

        private HelpCommand(CommandService commandService) => _commandService = commandService;

        [Command("help")]
        public Task ShowHelpAsync()
        {
            var embed = CreateEmbed(EmbedColor.SalmonPink)
                .WithAuthor("List of all commands", Context.Client.CurrentUser.GetAvatarUrl());

            // We may not need this for Maina
            var commandList = _commandService.Commands.Where(x => x.Module.Name != "Owner");

            foreach (var commands in commandList.GroupBy(x => x.Module.Name).OrderBy(y => y.Key))
            {
                embed.AddField(commands.Key,
                    $"`{string.Join("`, `", commands.Select(x => x.Module.Group ?? x.Name).Distinct())}`");
            }

            return ReplyAsync(string.Empty, embed.Build());
        }
    }
}
