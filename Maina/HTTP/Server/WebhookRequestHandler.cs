using Discord;
using Discord.WebSocket;
using Maina.Administrative;
using Maina.Core;
using Maina.Core.Logging;
using Maina.Database;
using Maina.HTTP.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Maina.HTTP.Server
{
	public class WebhookRequestHandler : RequestHandler
	{

		public override string Prefix {
			get { return "webhooks/"; }
		}

		
		public WebhookRequestHandler (DiscordSocketClient client, DatabaseManager database) : base(client, database) {

		}


		public override bool HandleRequest(HttpListenerContext context)
		{
			bool answered = false;
			HttpListenerRequest request = context.Request;
			
			try {
				if (!CheckTrustedAgent(context, out answered))
					return answered;

				else if (request.HttpMethod != "POST")
					answered = RespondToRequest(context, HttpStatusCode.MethodNotAllowed); //Method not allowed

				else if (!request.HasEntityBody)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else if (request.Headers.Get("X-Hub-Signature") == null)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest);

				else if (request.Headers.Get("X-GitHub-Event") == null)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else if (request.Headers.Get("X-GitHub-Event") != "release")
					answered = RespondToRequest(context, HttpStatusCode.NoContent); //Ok, but No Content

				else {

					string payload;
					Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
					using (StreamReader input = new StreamReader(request.InputStream, encoding)) {
						payload = input.ReadToEnd();
					
						input.Close();
					}

					if (!VerifySignature(request.Headers.Get("X-Hub-Signature"), encoding.GetBytes(payload)))
						answered = RespondToRequest(context, HttpStatusCode.Unauthorized);

					else {
						answered = RespondToRequest(context, HttpStatusCode.OK); //TODO send a proper response with a body
						ProcessPayload(payload);
					}
				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " request: " + e.Message);
			}
				
			return answered;
		}

		private async void ProcessPayload (string payload) {
			try {
				GitHubWebHookData ghdata = JsonConvert.DeserializeObject<GitHubWebHookData>(payload);

				if (ghdata.action == "released") {
					EmbedBuilder eb = null;
					eb = new EmbedBuilder { Color = new Color((uint) EmbedColor.SalmonPink) };
					eb.WithAuthor("I've heard some great news!");
					eb.WithThumbnailUrl(ghdata.repository.html_url + "/raw/master/thumbnail.png");
					eb.WithTitle(ghdata.release.name);
					eb.WithUrl(ghdata.release.html_url);
					eb.WithDescription("There is a new version of Miharu Available!");

					List<string> tags = new List<string>();
					tags.Add("Miharu");

					await DiscordAPIHelper.PublishNews(eb, _databaseManager, _discordSocketClient, tags.ToArray());
				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing GitHub release payload: " + e.Message);
			}
		}
		
		
	}
}
