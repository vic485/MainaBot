using Discord.WebSocket;
using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;
using Maina.HTTP.Data;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Maina.HTTP.Server
{
	public class RSSRequestHandler : RequestHandler
	{

		public override string Prefix {
			get { return "rss/"; }
		}


		public RSSRequestHandler (DiscordSocketClient client, DatabaseManager database) : base (client, database) {

		}

		

		public override bool HandleRequest(HttpListenerContext context)
		{
			bool answered = false;
			HttpListenerRequest request = context.Request;
			
			try {
				if (request.HttpMethod == "GET")
					answered = ProcessGet(context);

				else if (request.HttpMethod == "POST")
					answered = ProcessPost(context);
				
				else
					answered = RespondToRequest(context, HttpStatusCode.MethodNotAllowed); //Method not allowed
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " request: " + e.Message);
			}
				
			return answered;
		}





		private bool ProcessGet (HttpListenerContext context) {
			bool answered = false;
			HttpListenerRequest request = context.Request;

			try {
				RSSFeed[] feeds = null;
				if (!CheckTrustedAgent(context, out answered))
					return answered;

				else if (request.Headers.Get("Action") == null || request.Headers.Get("Action") != "List-Feeds")
					answered = RespondToRequest(context, HttpStatusCode.NotAcceptable); //Not acceptable
				
				else if ((feeds =_databaseManager.GetAll<RSSFeed>("https://mangadex.org/rss/")) == null)
					 answered = RespondToRequest(context, HttpStatusCode.NotFound);

				else {
					string body = JsonConvert.SerializeObject(feeds);
					answered = RespondToRequest(context, HttpStatusCode.OK, "application/json", body);
				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " GET request: " + e.Message);
			}

			return answered;
		}

		private bool ProcessPost (HttpListenerContext context) {
			bool answered = false;
			string payload = "";
			HttpListenerRequest request = context.Request;

			try {
				if (!CheckTrustedAgent(context, out answered))
					return answered;

				else if (!request.HasEntityBody)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else if (request.Headers.Get("Signature") == null)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest);

				else if (!(request.ContentType ?? "").Contains("application/json") || request.Headers.Get("Payload-Object") == null ||  request.Headers.Get("Payload-Object") != "RSS-Feed")
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else {
					Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
					using (StreamReader input = new StreamReader(request.InputStream, encoding)) {
						payload = input.ReadToEnd();

						input.Close();
					}

					if (!VerifySignature(request.Headers.Get("Signature"), encoding.GetBytes(payload)))
						answered = RespondToRequest(context, HttpStatusCode.Unauthorized);

					else {
						answered = RespondToRequest(context, HttpStatusCode.OK); //TODO send a proper response with a body
						ProcessPayload(payload);
					}
				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " POST request: " + e.Message);
			}

			return answered;
		}





		private void ProcessPayload (string payload) {
			try {
				RSSFeedData rssFeedData = JsonConvert.DeserializeObject<RSSFeedData>(payload);
				if (rssFeedData.Action == "Add") {
					if (rssFeedData.Id.StartsWith("https://mangadex.org/rss/")) {
						if (_databaseManager.Get<RSSFeed>(rssFeedData.Id) == null) {
							_databaseManager.Save<RSSFeed>(new RSSFeed { 
								Id = rssFeedData.Id,
								Tag = rssFeedData.Tag,
							});
							Logger.LogVerbose($"A new RSSFeed was added through webhooks ({rssFeedData.Id})");
						}

						else
							Logger.LogVerbose($"WebHook attempted to Add RSSFeed that already exists ({rssFeedData.Id}).");
					}

					else
						Logger.LogVerbose($"WebHook attempted to Add an invalid RSSFeed URL ({rssFeedData.Id}).");
				}

				else if (rssFeedData.Action == "Remove") {
					if (_databaseManager.Get<RSSFeed>(rssFeedData.Id) != null) {
						_databaseManager.Remove<RSSFeed>(new RSSFeed { Id = rssFeedData.Id });
						Logger.LogVerbose($"A RSSFeed was removed through webhooks ({rssFeedData.Id})");
					}

					else
						Logger.LogVerbose($"WebHook attempted to Remove RSSFeed that does not exist ({rssFeedData.Id}).");
				}

				else if (rssFeedData.Action == "Modify") {
					if (_databaseManager.Get<RSSFeed>(rssFeedData.Id) != null) {
						_databaseManager.Save<RSSFeed>(new RSSFeed { 
							Id = rssFeedData.Id,
							Tag = rssFeedData.Tag,
							LastUpdateId = rssFeedData.LastUpdateId,
						});
						Logger.LogVerbose($"A RSSFeed was modified through webhooks ({rssFeedData.Id})");
					}

					else
						Logger.LogVerbose($"WebHook attempted to Modify RSSFeed that does not exist ({rssFeedData.Id}).");
				}

				else
					Logger.LogVerbose("Unknown Action: " + rssFeedData.Action);
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " payload: " + e.Message);
			}
		}

		
	}
}
