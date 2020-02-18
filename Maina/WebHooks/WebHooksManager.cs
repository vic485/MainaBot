using Maina.WebHooks.Server;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Maina.Core.Logging;
using Discord;
using Maina.Database;
using Discord.WebSocket;
using Maina.Core;
using Maina.Administrative;
using System.Collections.Generic;
using Maina.WebHooks.Data;
using Maina.Database.Models;

namespace Maina.WebHooks
{
	public class WebHooksManager : IDisposable, WebHookIntermediary
	{


		private readonly DatabaseManager _dataBaseManager;
		private readonly DiscordSocketClient _discordSocketClient;

		private WebHookListener _listener = null;

		/// <summary>
		/// A newly created WebHooksManager is not listening to any requests. Use Initialize to start listening.
		/// </summary>
		/// <param name="receiveTo">An array with the prefixes for which to accept requests.
		/// <para>If null default prefixes are "http://*:8080/webhooks/" and "https://*:443/webhooks/"</para></param>
		public WebHooksManager (DiscordSocketClient client, DatabaseManager database) {
			_dataBaseManager = database;
			_discordSocketClient = client;
		}


		private EmbedBuilder GithubPayloadToEmbedBuilder (string payload) {
			EmbedBuilder eb = null;
			GitHubWebHookData ghdata = JsonConvert.DeserializeObject<GitHubWebHookData>(payload);
			eb = new EmbedBuilder { Color = new Color((uint) EmbedColor.SalmonPink) };
			eb.WithAuthor("I've heard some great news!");
			eb.WithThumbnailUrl("https://cdn.discordapp.com/attachments/677950856921874474/678657998637236266/Miharu_Bot_Final.png");
			eb.WithTitle(ghdata.release.name);
			eb.WithUrl(ghdata.release.html_url);
			eb.WithDescription("There is a new version of Miharu Available!");
			return eb;
		}
		
		private Tuple<EmbedBuilder, string []> JsonToEmbedBuilder (string payload) {
			EmbedBuilder eb = null;
			EmbedData edata = JsonConvert.DeserializeObject<EmbedData>(payload);
			eb = new EmbedBuilder { Color = new Color(edata.Color ?? (uint)EmbedColor.SalmonPink) };
			eb.Title = edata.Title;
			eb.Description = edata.Description;
			eb.Url = edata.URL;
			eb.ImageUrl = edata.IconURL;
			if (edata.Author != null && edata.Author != "") eb.WithAuthor(edata.Author, edata.AuthorIconURL, edata.AuthorURL);
			if (edata.Fields != null) {
				foreach (EmbedFieldData efdata in edata.Fields) {
					if ((efdata.Name ?? "") != "" && (efdata.Value ?? "") != "")
						eb.AddField(efdata.Name, efdata.Value, efdata.Inline);
				}
			}
			if ((edata.Footer ?? "") != "") eb.WithFooter(edata.Footer, edata.FooterIcon);


			return new Tuple<EmbedBuilder, string[]>(eb, edata.Tags);
		}

		private void ProcessRSSFeedPayload (string payload) {
			RSSFeedData rssFeedData = JsonConvert.DeserializeObject<RSSFeedData>(payload);
			if (rssFeedData.Action == "Add") {
				if (_dataBaseManager.Get<RSSFeed>(rssFeedData.Id) == null) {
					_dataBaseManager.Save<RSSFeed>(new RSSFeed { 
						Id = rssFeedData.Id,
						Tag = rssFeedData.Tag,
					});
					Logger.LogInfo($"A new RSSFeed was added through webhooks ({rssFeedData.Id})");
				}
				else
					throw new Exception($"WebHook attempted to Add RSSFeed that already exists ({rssFeedData.Id}).");
			}
			else if (rssFeedData.Action == "Remove") {
				if (_dataBaseManager.Get<RSSFeed>(rssFeedData.Id) != null) {
					_dataBaseManager.Remove<RSSFeed>(new RSSFeed { Id = rssFeedData.Id });
					Logger.LogInfo($"A RSSFeed was removed through webhooks ({rssFeedData.Id})");
				}
				else
					throw new Exception($"WebHook attempted to Remove RSSFeed that does not exist ({rssFeedData.Id}).");
			}
			else if (rssFeedData.Action == "Modify") {
				if (_dataBaseManager.Get<RSSFeed>(rssFeedData.Id) != null) {
					_dataBaseManager.Save<RSSFeed>(new RSSFeed { 
						Id = rssFeedData.Id,
						Tag = rssFeedData.Tag,
						LastUpdateId = rssFeedData.LastUpdateId,
					});
					Logger.LogInfo($"A RSSFeed was modified through webhooks ({rssFeedData.Id})");
				}
				else
					throw new Exception($"WebHook attempted to Modify RSSFeed that does not exist ({rssFeedData.Id}).");
			}
			else
				throw new Exception("Unknown Action: " + rssFeedData.Action);
		}

		public void OnPayloadReceived(PayloadType type, string payload)
		{
			try {
				EmbedBuilder eb = null;
				List<string> tags = new List<string>();
				if (type == PayloadType.GitHubRelease) {
					try {
						eb = GithubPayloadToEmbedBuilder(payload); 
						tags.Add("Miharu");
					}
					catch (Exception e) {
						Logger.LogError("Error parsing GitHub release payload: " + e.Message);
					}
				}
				else if (type == PayloadType.DiscordEmbed) {
					try {
						Tuple<EmbedBuilder, string []> result = JsonToEmbedBuilder(payload);
						eb = result.Item1;
						foreach (string tag in result.Item2)
							tags.Add(tag);
					}
					catch (Exception e) {
						Logger.LogError("Error parsing Json Embed payload: " + e.Message);
					}
				}
				else if (type == PayloadType.RSSFeed) {
					try {
						eb = null;
						ProcessRSSFeedPayload(payload);
					}
					catch(Exception e) {
						Logger.LogError ("Error processing RSSFeed payload: " + e.Message);
					}
				}
				else {
					eb = null;
					Logger.LogWarning("Received unknown payload:\n" + payload);
				}
				if (eb != null)
					_ = DiscordAPIHelper.PublishNews(eb, _dataBaseManager, _discordSocketClient, tags.ToArray());
			}
			catch(Exception e) {
				Logger.LogError("Failed to process webhook payload." + e.Message);
			}

		}

		public void OnWebHookListenFail(Exception e, bool stillAlive)
		{
			Logger.LogError("Error in WebHook server: " + e.Message);
			if (!stillAlive)
				ResetServer();
		}

		public void OnWebHookRequestProcessFail(Exception e)
		{
			Logger.LogError("Error in WebHook server: " + e.Message);
		}


		public RSSFeed[] GetRSSFeedList()
		{
			return _dataBaseManager.GetAll<RSSFeed>("https://mangadex.org/rss/");
		}




		#region WebHookListener Stuff

		private string [] _trustedUserAgents;
		/// <summary>
		/// Starts listening and logs the addresses it's listening to.
		/// </summary>
		/// <param name="receiveTo">The addresses to listen to.</param>
		public void Initialize (string [] receiveTo = null, string [] trustedUserAgents = null) {
			_listener = new WebHookListener(this, receiveTo);
			_trustedUserAgents = trustedUserAgents;
			if (_trustedUserAgents != null)
				_listener.TrustedUserAgents.AddRange(_trustedUserAgents);

			Task.Run(() => {
				try {
					_listener.StartListening();
				}
				catch (Exception) {
					Logger.LogError("Could not start HTTP server. Is the bot running with privileges?");
				}
			});

			string [] whURLs = _listener.GetWebHookURLs();
			if (whURLs != null) {
				string urls = "";
				foreach (string url in whURLs)
					urls += Environment.NewLine + "    " + url;
				Logger.LogInfo("WebHook Server Listening for HTTP requests on:" + urls);
			}
			else
				Logger.LogWarning("Could not retrieve public WebHook URLs.");
		}

		/// <summary>
		/// Resets the webhook listener, freeing any old resources.
		/// </summary>
		public void ResetServer () {
			_listener.StopListening();
			_listener = new WebHookListener(this);
			Task.Run(() => _listener.StartListening());
		}

		#endregion







		public void Dispose()
		{
			_listener?.Dispose();
			_listener = null;
		}

		
	}
}
