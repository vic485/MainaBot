using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Maina.Core;
using Maina.Database.Models;

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


		[Group("rss")]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		public class RSSSubCommand : MainaBase {
			[Command("list")]
			[RequireUserPermission(GuildPermission.ManageChannels)]
			public async Task BaseCommand()
			{
				EmbedBuilder eb = null;
				RSSFeed[] feeds = Context.Database.GetAll<RSSFeed>("https://mangadex.org/rss/");
				if (feeds == null || feeds.Length == 0) {
					eb = CreateEmbed(EmbedColor.Red);
					eb.WithAuthor("No available RSS Feeds :C");				
				}
				else {
					eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("List of available RSS Feeds");
					foreach (RSSFeed feed in feeds) {
						eb.AddField("Tag: " + feed.Tag, feed.Id);
					}
				}
				await ReplyAsync(string.Empty, eb.Build(), false, false);

			}

			[Command("add")]
			[RequireOwner]
			public async Task BaseCommand(string url, string tag)
			{
				RSSFeed feed = new RSSFeed { Id = url, Tag = tag };
				if (!Context.Database.Exists<RSSFeed>(feed)) {
					Context.Database.Save<RSSFeed>(feed);
					EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("Added RSS feed.");
					eb.WithDescription("RSS feeds are only polled every 60 seconds, be patient if no news appear inmediately.");
					await ReplyAsync(string.Empty, eb.Build(), false, false);
				}
				else {
					EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("I'm already subscribed to that RSS feed.");
					await ReplyAsync(string.Empty, eb.Build(), false, false);
				}

			}

			
			[Command("remove")]
			[RequireOwner]
			[RequireUserPermission(GuildPermission.ManageChannels)]
			public async Task BaseCommand(string url)
			{
				RSSFeed feed = new RSSFeed { Id = url };
				if(Context.Database.Exists<RSSFeed>(feed)) {
					Context.Database.Remove<RSSFeed>(feed);
					EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("Removed RSS feed.");
					await ReplyAsync(string.Empty, eb.Build(), false, false);
				}
				else {
					EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("I'm not subscribed to that RSS feed.");
					await ReplyAsync(string.Empty, eb.Build(), false, false);
				}


			}
		}



		[Group("role")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		public class RoleSubCommand : MainaBase {
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

			[Command("remove")]
			[RequireUserPermission(GuildPermission.ManageRoles)]
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
			[RequireUserPermission(GuildPermission.ManageRoles)]
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
