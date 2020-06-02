using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Database.Models;

namespace Maina.Administrative.Commands
{


	[Name("Administrative"), Group("news")]
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
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"No news channel set.",
					Context.HttpServerManager.GetIp + "/images/error.png");
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






		[Group("role")] // TODO: nested groups cause issues with help command
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public class RoleSubCommand : MainaBase {
			[Command("list")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task BaseCommand()
			{
				GuildConfig gc = Context.GuildConfig;
				SocketGuild guild = Context.Guild;
				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
				eb.WithAuthor($"List of news roles.");

				bool atLeastOneRole = false;
				if (gc.AllNewsRole.HasValue) {
					SocketRole role = guild.GetRole(gc.AllNewsRole.Value);
					eb.AddField("All News", role.Mention, true);
					atLeastOneRole = true;
				}
				foreach (string tag in gc.NewsRoles.Keys) {
					SocketRole role = guild.GetRole(gc.NewsRoles[tag]);
					eb.AddField("Tag: " + tag, role.Mention, true);
					atLeastOneRole = true;
				}

				if (atLeastOneRole)
					await ReplyAsync(string.Empty, eb.Build(), false, true);
				else {
					await DiscordAPIHelper.ReplyWithError(Context.Message, 
						"No roles assigned to tags.",
						Context.HttpServerManager.GetIp + "/images/error.png");
				}
			}




			[Command("add")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task BaseCommand(SocketRole role)
			{
				Context.GuildConfig.AllNewsRole = role.Id;

				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
				eb.WithAuthor($"All News role updated!");
				eb.WithDescription($"I will ping {role.Mention} for all news.");

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}

			[Command("add")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public async Task BaseCommand(string tag, SocketRole role)
			{
				Context.GuildConfig.NewsRoles[tag] = role.Id;

				EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
				eb.WithAuthor($"News role updated!");
				eb.WithDescription($"I will ping {role.Mention} for news with {tag} tag.");

				await ReplyAsync(string.Empty, eb.Build(), false, true);

			}

			[Group("remove")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
			public class RemoveSubCommand : MainaBase{
				[Command]
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
						await DiscordAPIHelper.ReplyWithError(Context.Message, 
							"There is no role for all news.",
							Context.HttpServerManager.GetIp + "/images/error.png");
						return;
					}

					await ReplyAsync(string.Empty, eb.Build(), false, true);

				}

				[Command]
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
						await DiscordAPIHelper.ReplyWithError(Context.Message, 
							"There is no role linked to that tag.",
							Context.HttpServerManager.GetIp + "/images/error.png");
					}

					await ReplyAsync(string.Empty, eb.Build(), false, true);

				}
			}
		}

	}
}
