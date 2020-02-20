using Discord;
using Discord.WebSocket;
using Maina.Administrative;
using Maina.Core;
using Maina.Database;
using System;

namespace Maina.RSS
{
	public class RSSManager : IDisposable
	{


		private RSSClient _rssClient;

		private readonly DatabaseManager _databaseManager;
		private readonly DiscordSocketClient _discordSocketClient;

		public RSSManager (DiscordSocketClient client, DatabaseManager database) {
			_databaseManager = database;
			_discordSocketClient = client;
		}


		public void Initialize () {
			_rssClient = new RSSClient(_databaseManager);
			_rssClient.RSSUpdate += OnRSSUpdateAsync;
			_rssClient.RSSError += OnRSSError;
			_rssClient.Start();

		}

		

		private async void OnRSSUpdateAsync(object sender, RSSUpdateEventArgs e)
		{
			EmbedBuilder eb = new EmbedBuilder { Color = new Color((uint) EmbedColor.SalmonPink) };
			eb.WithAuthor("I've heard some great news!");
			//TODO Add manga cover
			//eb.WithThumbnailUrl("https://cdn.discordapp.com/attachments/677950856921874474/678657998637236266/Miharu_Bot_Final.png");
			eb.WithTitle(e.Update.Title.Text);
			eb.WithUrl(e.Update.Id);
			eb.WithDescription($"There is a new chapter available to read!");
			await DiscordAPIHelper.PublishNews(eb, _databaseManager, _discordSocketClient, e.Feed.Tag);
		}


		private void OnRSSError(object sender, RSSErrorEventArgs e)
		{
			if (!e.StillAlive){
				_rssClient = new RSSClient(_databaseManager);
				_rssClient.RSSUpdate += OnRSSUpdateAsync;
				_rssClient.RSSError += OnRSSError;
				_rssClient.Start();
			}
		}

		public void Dispose()
		{
			_rssClient?.Dispose();
			_rssClient = null;
		}
	}
}
