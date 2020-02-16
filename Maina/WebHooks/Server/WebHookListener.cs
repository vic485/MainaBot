using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Maina.WebHooks.Server
{

	public enum PayloadType {
		GitHubRelease,
		Unknown
	}

	public class WebHookListener : IDisposable
	{
		


		private AutoResetEvent KeepWorking = new AutoResetEvent(false);

		private ManualResetEvent ListenerStopped = new ManualResetEvent(false);

		private HttpListener _listener = null;
		private WebHookObserver _observer;
		
		public List<string> TrustedUserAgents {
			get; private set;
		} = new List<string>();



		/// <summary>
		/// Creates a new WebHookListener. To start listening call StartListening.
		/// </summary>
		/// <param name="observer">The object with the callback methods.</param>
		/// <param name="receiveTo">An array with the prefixes for which to accept requests.
		/// <para>If null default prefixes are "http://*:8080/webhooks/" and "https://*:443/webhooks/"</para></param>
		public WebHookListener (WebHookObserver observer, string [] receiveTo = null) {
			TrustedUserAgents.Add("GitHub-Hookshot");


			if (receiveTo == null) {
				receiveTo = new string[2];
				receiveTo[0] = "http://*:8080/webhooks/";
				receiveTo[1] = "https://*:443/webhooks/";
			}
			_listener = new HttpListener();
			foreach (string prefix in receiveTo)
				_listener.Prefixes.Add(prefix);
			_observer = observer;
		}


		/// <summary>
		/// </summary>
		/// <returns>An array with the public WebHook URLs or null if it fails.</returns>
		public string [] GetWebHookURLs () {
			try {
				string ip;
				using (WebClient wc = new WebClient())
					ip = wc.DownloadString("http://ipinfo.io/ip");
				ip = ip.TrimEnd();
				string [] urls = new string [_listener.Prefixes.Count];
				int i = 0; 
				foreach (string p in _listener.Prefixes) {
					urls[i] = p.Replace("*", ip);
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

					_listener.BeginGetContext(new AsyncCallback(RetrieveRequest), _listener);

					KeepWorking.WaitOne();
				}
			}
			catch (Exception e) {
				_observer?.OnWebHookListenFail(e, false);
			}
			finally {
				if (_listener.IsListening) {
					_listener?.Stop();
					_listener?.Close();
				}
				_listener = null;
				ListenerStopped.Set();
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

		private bool RespondWithError (int error, HttpListenerContext context) {
			using (HttpListenerResponse failResponse = context.Response) {
				failResponse.StatusCode = error;
				failResponse.Close();
			}
			return true;
		}


		private void RetrieveRequest(IAsyncResult ar)
		{
			KeepWorking.Set();

			bool release = false;
			string payload = null;
			bool answered = false;
			HttpListener listener = (HttpListener)ar.AsyncState;
			HttpListenerContext context = listener.EndGetContext(ar);

			try {
				
				HttpListenerRequest request = context.Request;
				
				if (request.HttpMethod != "POST") {
					RespondWithError(405, context); //Method not allowed
					return;
				}					
				if (request.UserAgent == null || TrustedUserAgents.Find(x => request.UserAgent.Contains(x)) == null) {
					RespondWithError(401, context); //Unauthorized
					return;
				}
				if (!request.HasEntityBody) {
					RespondWithError(400, context); //Bad Request
					return;
				}


				if (request.Headers.Get("X-GitHub-Event") != null)
					release = request.Headers.Get("X-GitHub-Event") == ("release");

					
				using (StreamReader input = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8)) {
					payload = input.ReadToEnd();

					input.Close();
				}

				HttpListenerResponse response = context.Response;
				/*string responseString = "";
				byte [] buff = Encoding.UTF8.GetBytes(responseString);
				response.ContentLength64 = buff.Length;*/

				response.StatusCode = 200; //OK
				response.Close();
				answered = true;

				/*using (Stream output = response.OutputStream) {
					output.Write(buff, 0, buff.Length);
					output.Close();
				}*/
				
					
			}
			catch(Exception e) {
				answered = RespondWithError(500, context); //Internal Server Error
				_observer?.OnWebHookRequestProcessFail(e);
			}
			

			if (!answered){
				try {
					RespondWithError(500, context); //Internal Server Error
				}
				catch (Exception e) {
					_observer?.OnWebHookRequestProcessFail(e);
				}
			}


			if (payload != null) {
				if (release)
					_observer?.OnPayloadReceived(PayloadType.GitHubRelease, payload);
				else
					_observer?.OnPayloadReceived(PayloadType.Unknown, payload);
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
				KeepWorking.Set();
				_listener?.Abort();
				_listener = null;				
				KeepWorking.Dispose();
				ListenerStopped.Dispose();
				
				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~WebHookListener()
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
