using System;
using System.Collections.Generic;
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
				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
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
			
			EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
			eb.WithAuthor("News channel set!");
			eb.WithDescription(channel.Mention);

			await ReplyAsync(string.Empty, eb.Build(), false, true);

		}

		[Group("role")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public class RoleSubCommand : MainaBase {
			[Command("add")]			
			public async Task BaseCommand(SocketRole role)
			{
				Context.GuildConfig.AllNewsRole = role.Id;
			
				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
				eb.WithAuthor($"All News role updated!");
				eb.WithDescription($"I will ping {role.Mention} for all news.");

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}
			
			[Command("add")]
			public async Task BaseCommand(string tag, SocketRole role)
			{
				Context.GuildConfig.NewsRoles[tag] = role.Id;
			
				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
				eb.WithAuthor($"News role updated!");
				eb.WithDescription($"I will ping {role.Mention} for news with {tag} tag.");

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}

			[Command("remove")]
			public async Task BaseCommand()
			{
				EmbedBuilder eb = null;
				if (Context.GuildConfig.AllNewsRole.HasValue) {
					SocketRole role = Context.Guild.GetRole(Context.GuildConfig.AllNewsRole.Value);
					Context.GuildConfig.AllNewsRole = null;
					eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor($"News role removed!");
					eb.WithDescription($"I will no longer ping {role.Mention} for all news.");
				}
				else {
					eb = CreateEmbed(EmbedColor.Red);
					eb.WithAuthor($"There was no role for all news.");
				}

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}

			[Command("remove")]
			public async Task BaseCommand(string tag)
			{
				EmbedBuilder eb = null;
				if (Context.GuildConfig.NewsRoles.ContainsKey(tag)) {
					SocketRole role = Context.Guild.GetRole(Context.GuildConfig.NewsRoles[tag]);
					Context.GuildConfig.NewsRoles.Remove(tag);
					eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor($"News role removed!");
					eb.WithDescription($"I will no longer ping {role.Mention} for news with {tag} tag.");
				}
				else {
					eb = CreateEmbed(EmbedColor.Red);
					eb.WithAuthor($"There is no role linked to that tag.");
				}

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}
		}

	}
}
