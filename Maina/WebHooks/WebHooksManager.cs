using Maina.WebHooks.Server;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Maina.Core.Logging;
using Discord;
using Maina.Database;
using Discord.WebSocket;
using Maina.Core;
using Maina.Database.Models;

namespace Maina.WebHooks
{
	public class WebHooksManager : IDisposable, WebHookObserver
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



		public void OnPayloadReceived(PayloadType type, string payload)
		{

			EmbedBuilder eb = null;

			if (type == PayloadType.GitHubRelease) {
				try {
					GitHubWebHookData ghdata = JsonConvert.DeserializeObject<GitHubWebHookData>(payload);
					eb = new EmbedBuilder { Color = new Color((uint) EmbedColor.Aqua) };
					eb.WithAuthor("I've heard some great news!");
					eb.WithThumbnailUrl("https://cdn.discordapp.com/attachments/677950856921874474/678657998637236266/Miharu_Bot_Final.png");
					eb.WithTitle(ghdata.release.name);
					eb.WithUrl(ghdata.release.html_url);
					eb.WithDescription("There is a new version of Miharu Available!");
				}
				catch (Exception e) {
					Logger.LogError("Error parsing GitHub release payload: " + e.Message);
				}
			}
			else {
				eb = new EmbedBuilder {Color = new Color((uint) EmbedColor.Green) };
				eb.WithAuthor("I've heard some great news!");
				eb.WithTitle(payload);
			}

			if (eb != null) {
				foreach (GuildConfig gc in _dataBaseManager.GetAllGuilds()) {
					try {
						SocketGuild guild = _discordSocketClient.GetGuild(gc.NumberId);
						if (gc.NewsChannel.HasValue) {
							SocketTextChannel channel = guild.GetTextChannel(gc.NewsChannel.Value);
							channel.SendMessageAsync(string.Empty, false, eb.Build());
						}
					}
					catch (Exception){ }
				}
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
					urls += Environment.NewLine + url;
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
