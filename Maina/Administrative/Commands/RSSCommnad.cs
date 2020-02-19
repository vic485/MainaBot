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

}
