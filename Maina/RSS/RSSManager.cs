using Discord;
using Discord.WebSocket;
using Maina.Administrative;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database;
using System;
using System.Net;

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
			eb.WithTitle(e.Update.Title.Text);
			eb.WithUrl(e.Update.Id);
			eb.WithFooter($"There is a new chapter available to read!");

			int idIndex = e.Feed.Id.LastIndexOf("/") +1;
			int id = -1;
			string imageUrl = null;
			if (int.TryParse(e.Feed.Id.Substring(idIndex), out id))
				imageUrl = $"https://mangadex.org/images/manga/{id}.";
			if (imageUrl != null) {
				if (DoesImageExist(ref imageUrl))
					eb.WithImageUrl(imageUrl);
			}

			await DiscordAPIHelper.PublishNews(eb, _databaseManager, _discordSocketClient, e.Feed.Tag);
		}

		private static readonly string [] _COVER_EXTENSIONS = { "jpg", "jpeg", "png" };
		private bool DoesImageExist (ref string baseUrl) {
			bool exists = false;
			for (int i = 0; i < _COVER_EXTENSIONS.Length && !exists; i++) {
				try {
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + _COVER_EXTENSIONS[i]);
					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
						if (exists = (response.StatusCode == HttpStatusCode.OK))
							baseUrl = baseUrl + _COVER_EXTENSIONS[i];
					}
				}
				catch (Exception) {}
			}
			return exists;
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
