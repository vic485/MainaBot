using System.Collections.Generic;
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
        public async Task ShowHelpAsync(string commandThatNeedsHelp = null)
        {
            string title = commandThatNeedsHelp == null ? "List of all commands" : ("List of commands for " + commandThatNeedsHelp);
            var embed = CreateEmbed(EmbedColor.SalmonPink)
                .WithAuthor(title, Context.Client.CurrentUser.GetAvatarUrl());

                        
            IEnumerable<CommandInfo> commandList;
            var owner = (await Context.Client.GetApplicationInfoAsync()).Owner;
            if (owner == Context.User)
                commandList = _commandService.Commands;
            else
                commandList = _commandService.Commands.Where(x => x.Module.Name != "Owner");


            if (commandThatNeedsHelp == null) {
                foreach (var commands in commandList.Where(
                        w => w.Module.Parent == null)
                        .GroupBy(x => x.Module.Name)
                        .OrderBy(y => y.Key))
                {
                    var group = commands.Select(x => x.Module.Group ?? x.Name).Distinct();
                    embed.AddField(commands.Key,
                        $"`{string.Join("`, `", group)}`");
                }
            }
            else {
                foreach (var commands in commandList.Where(
                        w => w.Module.Group == commandThatNeedsHelp ||w.Module.Parent != null && w.Module.Parent.Group == commandThatNeedsHelp)
                        .GroupBy(x => x.Module.Name)
                        .OrderBy(y => y.Key))
                {
                    var group = commands.Select(x => x.Name).Distinct();
                    embed.AddField(commands.Key,
                        $"`{string.Join("`, `", group)}`");
                }
            }

            ReplyAsync(string.Empty, embed.Build());
            
        }

    }
}
