using Maina.HTTP.Server;
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
using Maina.HTTP.Data;
using Maina.Database.Models;

namespace Maina.HTTP
{
	public class HTTPServerManager : IDisposable
	{


		private readonly DiscordSocketClient _discordSocketClient;
		private readonly DatabaseManager _databaseManager;		

		private Server.HTTPServer _listener = null;

		/// <summary>
		/// A newly created WebHooksManager is not listening to any requests. Use Initialize to start listening.
		/// </summary>
		/// <param name="receiveTo">An array with the prefixes for which to accept requests.
		/// <para>If null default prefixes are "http://*:8080/webhooks/" and "https://*:443/webhooks/"</para></param>
		public HTTPServerManager (DiscordSocketClient client, DatabaseManager database) {
			_discordSocketClient = client;
			_databaseManager = database;
		}


		public void ChangeAgents(List<string> agentList)
		{
			_listener?.TrustedUserAgents.Clear();
			foreach (var agent in agentList)
			{
				_listener?.TrustedUserAgents.Add(agent);
			}
		}
			


		private string [] _trustedUserAgents;
		/// <summary>
		/// Starts listening and logs the addresses it's listening to.
		/// </summary>
		/// <param name="receiveTo">The addresses to listen to.</param>
		public void Initialize (string [] receiveTo = null, string [] trustedUserAgents = null) {
			_trustedUserAgents = trustedUserAgents;
			ResetServer();

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
			try { _listener?.StopListening(); }	catch (Exception) { }

			_listener = new HTTPServer (8080, _discordSocketClient, _databaseManager);
			_listener.Error += OnHTTPServerError;
			if (_trustedUserAgents != null)
				_listener.TrustedUserAgents.AddRange(_trustedUserAgents);
			Task.Run(() => _listener.StartListening());
		}


		private void OnHTTPServerError(object sender, HTTPServerEventArgs e)
		{
			Logger.LogError((e.StillAlive ? "E" : "Fatal e") + "rror during HTTP server execution: " + e.Exception.Message);
			if (!e.StillAlive) {
				Logger.LogVerbose("Rebooting server...");
				_listener = null;
				ResetServer();
			}
		}


		public void Dispose()
		{
			_listener?.Dispose();
			_listener = null;
		}

		
	}
}
