
using Discord.WebSocket;
using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Maina.HTTP.Server
{

	

	public abstract class RequestHandler
	{
		protected DiscordSocketClient _discordSocketClient;
		protected DatabaseManager _databaseManager;

		public abstract string Prefix {
			get;
		}

		public RequestHandler (DiscordSocketClient client, DatabaseManager database) {
			_databaseManager = database;
			_discordSocketClient = client;
		}

		
		public abstract bool HandleRequest(HttpListenerContext context);

		public static bool RespondToRequest(HttpListenerContext context, HttpStatusCode code, string contentType = "text/plain", string body = null) {
			try {
				HttpListenerResponse response = context.Response;
				response.StatusCode = (int)code;
				response.ContentType = contentType;
			
				if (body != null) {
					byte [] buff = Encoding.UTF8.GetBytes(body);
					response.ContentLength64 = buff.Length;
					using (Stream output = response.OutputStream) {
						output.Write(buff, 0, buff.Length);
						output.Close();
					}
				}
				else
					response.Close();
				return true;
			}
			catch (Exception) {
				return false;
			}
		}

		public bool VerifySignature (string receivedSignature, Stream payload) {
			string token = _databaseManager.Get<BotConfig>("Config").SecretToken;
			HMACSHA1 hmac = new HMACSHA1(Encoding.ASCII.GetBytes(token));
			hmac.Initialize();

			string signature = BitConverter.ToString(hmac.ComputeHash(payload));
			signature = signature.Replace("-", "");
			signature = signature.ToLower();
			signature = "sha1=" + signature;

			Logger.LogForce("Signatures did" + (signature == receivedSignature ? "" : "'nt") + " match!");
			Logger.LogForce($"Received signature: {receivedSignature}");
			Logger.LogForce($"Hashed signature: {signature}");

			return true;
		}


	}
}
