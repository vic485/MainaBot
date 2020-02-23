using Discord.WebSocket;
using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Maina.HTTP.Server
{

	

	public class HTTPServer : IDisposable
	{

		public event HTTPServerEventHandler Error;



		private AutoResetEvent KeepWorking = new AutoResetEvent(false);

		private ManualResetEvent ListenerStopped = new ManualResetEvent(false);

		private HttpListener _listener = null;

		


		private Dictionary<string, RequestHandler> _requestHandlers = new Dictionary<string, RequestHandler>();

		private const string _ADDRESS_START = "http://*:";

		public string IP {
			get; private set;
		}
		public int Port {
			get; private set;
		}


		/// <summary>
		/// Creates a new WebHookListener. To start listening call StartListening.
		/// </summary>
		/// <param name="observer">The object with the callback methods.</param>
		/// <param name="receiveTo">An array with the prefixes for which to accept requests.
		/// <para>If null default prefixes are "http://*:8080/webhooks/" and "https://*:443/webhooks/"</para></param>
		public HTTPServer (int port, DiscordSocketClient client, DatabaseManager database) {
			Port = port;
			try {
				using (WebClient wc = new WebClient())
					IP = wc.DownloadString("http://ipinfo.io/ip");
				IP = IP.TrimEnd();
			}catch (Exception) {}

			_listener = new HttpListener();

			Type[] requestHandlers = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
					from assemblyType in domainAssembly.GetTypes()
					where assemblyType.IsSubclassOf(typeof(RequestHandler)) && !assemblyType.IsAbstract
					select assemblyType).ToArray();

			foreach (Type t in requestHandlers) {
				RequestHandler rh = (RequestHandler)Activator.CreateInstance(t, client, database);
				_listener.Prefixes.Add(_ADDRESS_START + port + "/" + rh.Prefix);
				_requestHandlers.Add(rh.Prefix, rh);
			}
		}

		

		/// <summary>
		/// </summary>
		/// <returns>An array with the public WebHook URLs or null if it fails.</returns>
		public string [] GetWebHookURLs () {
			try {
				string [] urls = new string [_listener.Prefixes.Count];
				int i = 0;
				foreach (string p in _listener.Prefixes) {
					urls[i] = p.Replace("*", IP);
					i++;
				}

				return urls;
			}
			catch (Exception) {
				return null;
			}
		}



		private bool _end = false;
		/// <summary>
		/// Starts the listening process.
		/// <para>This call is blocking, it is recommended to use Task.Run or another threading mechanism to call this function.</para>
		/// </summary>
		public void StartListening () {

			_listener.Start();
			try {
				while (!_end) {

					_listener?.BeginGetContext(new AsyncCallback(RetrieveRequest), _listener);

					KeepWorking?.WaitOne();
				}
			}
			catch (Exception e) {
				Error?.Invoke(this, new HTTPServerEventArgs(e, false));
			}
			finally {
				ListenerStopped?.Set();
				Dispose();
			}
		}

		/// <summary>
		/// Stops the process listening. Pending requests may still be processed, this is not guaranteed.
		/// </summary>
		public void StopListening () {
			if (!_end) {
				_end = true;
				KeepWorking.Set();
				ListenerStopped.WaitOne();
			}
		}

		


		private void RetrieveRequest(IAsyncResult ar)
		{

			bool answered = false;
			HttpListener listener = (HttpListener)ar.AsyncState;
			HttpListenerContext context = listener.EndGetContext(ar);
			//Keep working after we have the context??
			KeepWorking.Set();

			try {

				HttpListenerRequest request = context.Request;
				string handlerPrefix = request.Url.Segments[1];
				if (!handlerPrefix.EndsWith("/"))
					handlerPrefix += "/";

				if (!_requestHandlers.ContainsKey(handlerPrefix))
					answered = RequestHandler.RespondToRequest(context, HttpStatusCode.NotFound); //Not Found
				
				else
					answered = _requestHandlers[handlerPrefix].HandleRequest(context);
				 
			}
			catch(Exception e) {
				if (!answered) {
					answered = RequestHandler.RespondToRequest(context, HttpStatusCode.InternalServerError); //Internal Server Error
					Logger.LogError("There was an error processing a HTPP request: " + e.Message);
				}
			}
			finally {
				if (!answered){
					try {
						answered = RequestHandler.RespondToRequest (context, HttpStatusCode.InternalServerError); //Internal Server Error
					}
					catch (Exception e) {
						Logger.LogError("There was an error processing a HTPP request: " + e.Message);
					}
				}
			}
		}

		


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{

				}
				_end = true;
				KeepWorking?.Set();

				_listener?.Abort();
				//_listener?.Dispose();
				_listener = null;

				KeepWorking.Dispose();
				KeepWorking= null;
				ListenerStopped?.Dispose();
				ListenerStopped = null;

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~HTTPServer()
		{
		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		/// <summary>
		/// Forces the listening process to end, freeing all resources and all pending requests are discarded.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}


		#endregion
	}
}
