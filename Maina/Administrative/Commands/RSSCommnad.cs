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
	[Name("Administrative"), Group("rss")]
	[RequireUserPermission(GuildPermission.ManageChannels)]
	public class RSSSubCommand : MainaBase {
		[Command("list")]
		[RequireUserPermission(GuildPermission.ManageChannels)]
		public async Task BaseCommand()
		{
			EmbedBuilder eb = null;
			RSSFeed[] feeds = Context.Database.GetAll<RSSFeed>("https://mangadex.org/rss/");
			if (feeds == null || feeds.Length == 0) {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"No available RSS Feeds.",
					Context.HttpServerManager.GetIp + "/images/error.png");
				return;
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
			if (url.StartsWith("https://mangadex.org/rss/")) {
				RSSFeed feed = new RSSFeed { Id = url, Tag = tag };
				if (!Context.Database.Exists<RSSFeed>(feed)) {
					Context.Database.Save<RSSFeed>(feed);
					EmbedBuilder eb = CreateEmbed(EmbedColor.SalmonPink);
					eb.WithAuthor("Added RSS feed.");
					eb.WithDescription("RSS feeds are only polled every 60 seconds, be patient if no news appears immediately.");
					await ReplyAsync(string.Empty, eb.Build(), false, false);
				}
				else {
					await DiscordAPIHelper.ReplyWithError(Context.Message, 
						"I'm already subscribed to that RSS feed.",
						Context.HttpServerManager.GetIp + "/images/error.png");
				}
			}
			else {
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"That's not a valid RSS feed URL.",
					Context.HttpServerManager.GetIp + "/images/error.png");
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
				await DiscordAPIHelper.ReplyWithError(Context.Message, 
					"I'm not subscribed to that RSS feed.",
					Context.HttpServerManager.GetIp + "/images/error.png");
			}


		}
	}

}
