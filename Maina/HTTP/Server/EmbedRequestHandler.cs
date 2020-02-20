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
	public class EmbedRequestHandler : RequestHandler
	{
		public override string Prefix {
			get { return "/embed/"; }
		}

		public EmbedRequestHandler (DiscordSocketClient client, DatabaseManager database) : base(client, database) {

		}

		public override bool HandleRequest(HttpListenerContext context)
		{
			bool answered = false;
			string payload = "";
			HttpListenerRequest request = context.Request;
			
			try {
				if (request.HttpMethod != "POST")
					answered = RespondToRequest(context, HttpStatusCode.MethodNotAllowed); //Method not allowed

				else if (!request.HasEntityBody)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else if (!(request.ContentType ?? "").Contains("application/json") || request.Headers.Get("Payload-Object") == null ||  request.Headers.Get("Payload-Object") != "Discord-Embed")
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else {
					using (StreamReader input = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8)) {
						payload = input.ReadToEnd();

						input.Close();
					}
				
					answered = RespondToRequest(context, HttpStatusCode.OK); //TODO send a proper response with a body
					ProcessPayload(payload);
				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " request: " + e.Message);
			}
				
			return answered;
		}

		private async void ProcessPayload(string payload)
		{
			try {
				EmbedData embedData = JsonConvert.DeserializeObject<EmbedData>(payload);

				EmbedBuilder eb = null;
				eb = new EmbedBuilder { Color = new Color(embedData.Color ?? (uint)EmbedColor.SalmonPink) };
				eb.Title = embedData.Title;
				eb.Description = embedData.Description;
				eb.Url = embedData.URL;
				eb.ImageUrl = embedData.IconURL;
				if (embedData.Author != null && embedData.Author != "") eb.WithAuthor(embedData.Author, embedData.AuthorIconURL, embedData.AuthorURL);
				if (embedData.Fields != null) {
					foreach (EmbedFieldData efdata in embedData.Fields) {
						if ((efdata.Name ?? "") != "" && (efdata.Value ?? "") != "")
							eb.AddField(efdata.Name, efdata.Value, efdata.Inline);
					}
				}
				if ((embedData.Footer ?? "") != "") eb.WithFooter(embedData.Footer, embedData.FooterIcon);

				List<string> tags = new List<string>();
				foreach (string tag in embedData.Tags)
						tags.Add(tag);
			
				await DiscordAPIHelper.PublishNews(eb, _databaseManager, _discordSocketClient, tags.ToArray());
			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " payload: " + e.Message);
			}
		}
	}
}
