using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;

namespace Maina.General.Commands
{
	[Group("news")]
	public class NewsCommand : MainaBase
	{
		[Command("channel")]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		 public async Task BaseCommand()
        {
			if (Context.GuildConfig.NewsChannel.HasValue) {
				SocketTextChannel channel = Context.Guild.GetTextChannel(Context.GuildConfig.NewsChannel.Value);
				EmbedBuilder eb = CreateEmbed(EmbedColor.Green);
				eb.WithAuthor("News channel:");
				eb.WithDescription(channel.Mention);

				await ReplyAsync(string.Empty, eb.Build(), false, false);
			}
			else {
				EmbedBuilder eb = CreateEmbed(EmbedColor.Red);
				eb.WithAuthor("No news channel set :C");
				await ReplyAsync(string.Empty, eb.Build(), false, false);
			}
		}

		[Command("channel")]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		 public async Task BaseCommand(SocketTextChannel channel)
        {
			Context.GuildConfig.NewsChannel = channel.Id;
			
			EmbedBuilder eb = CreateEmbed(EmbedColor.Green);
			eb.WithAuthor("News channel set!");
			eb.WithDescription(channel.Mention);

			await ReplyAsync(string.Empty, eb.Build(), false, true);

		}


	}
}
