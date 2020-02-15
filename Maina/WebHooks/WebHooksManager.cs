﻿using Maina.WebHooks.Server;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Maina.Core.Logging;

namespace Maina.WebHooks
{
	public class WebHooksManager : IDisposable, WebHookObserver
	{
		#region Events
		/// <summary>
		/// The event fired when a payload is received.
		/// </summary>
		public event WebHookPayloadEventHandler PayloadReceived;
		/// <summary>
		/// The event fired if there was an exception in the webhook server.
		/// </summary>
		public event WebHookFailEventHandler SystemFailed;

		#endregion


		private WebHookListener _listener = null;

		/// <summary>
		/// A newly created WebHooksManager is not listening to any requests. Use Initialize to start listening.
		/// </summary>
		/// <param name="receiveTo">An array with the prefixes for which to accept requests.
		/// <para>If null default prefixes are "http://*:8080/webhooks/" and "https://*:443/webhooks/"</para></param>
		public WebHooksManager (string [] receiveTo = null) {
			_listener = new WebHookListener(this, receiveTo);
			
		}

		/// <summary>
		/// Starts listening and logs the addresses it's listening to.
		/// </summary>
		public void Initialize () {
			Task.Run(() => _listener.StartListening());
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





		public void OnPayloadReceived(PayloadType type, string payload)
		{
						
			if (type == PayloadType.GitHubRelease) {
				try {
					GitHubWebHookData ghdata = JsonConvert.DeserializeObject<GitHubWebHookData>(payload);
					PayloadReceived?.Invoke(this, new WebHookPayloadEventArgs(ghdata));
				}
				catch (Exception e) {
					//TODO probably wanna log this
					PayloadReceived?.Invoke(this, new WebHookPayloadEventArgs(payload));
				}
			}
			else			
				PayloadReceived?.Invoke(this, new WebHookPayloadEventArgs(payload));
			
		}

		public void OnWebHookListenFail(Exception e)
		{
			SystemFailed?.Invoke(this, new WebHookFailEventArgs(e, false));
		}

		public void OnWebHookRequestProcessFail(Exception e)
		{
			SystemFailed?.Invoke(this, new WebHookFailEventArgs(e, true));
		}






		public void Dispose()
		{
			_listener?.Dispose();
			_listener = null;
		}

		
	}
}
