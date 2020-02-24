using Discord.WebSocket;
using Maina.Core.Logging;
using Maina.Database;
using System;
using System.IO;
using System.Net;

namespace Maina.HTTP.Server
{
	public class ImageRequestHandler : RequestHandler
	{
		public override string Prefix {
			get { return "images/"; }
		}

		private const string _CONTAINER_FOLDER = "./images";
		public ImageRequestHandler (DiscordSocketClient client, DatabaseManager database) : base(client, database) {
			if (!Directory.Exists(_CONTAINER_FOLDER))
				Directory.CreateDirectory(_CONTAINER_FOLDER);
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

		private bool ProcessGet(HttpListenerContext context)
		{
			bool answered = false;
			HttpListenerRequest request = context.Request;

			try {
				string relativePath = "";
				if (!EnsurePathIsContained(request.RawUrl, out relativePath))
					answered = RespondToRequest(context, HttpStatusCode.NotFound);

				else if (!File.Exists(relativePath))
					answered = RespondToRequest(context, HttpStatusCode.NotFound);

				else {
					answered = RespondToRequest(context,
						HttpStatusCode.OK,
						File.ReadAllBytes(relativePath),
						new FileInfo(relativePath).Extension);
				}

			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " GET request: " + e.Message);
			}

			return answered;
		}

		private bool EnsurePathIsContained (string path, out string relativePath) {
			bool isContained = false;
			relativePath = null;
			try {
				string dirLU = "images/";
				path = path.Substring(path.IndexOf(dirLU) + dirLU.Length);
				relativePath = _CONTAINER_FOLDER + "/" + path;
				DirectoryInfo dTarget = new DirectoryInfo(relativePath);

				DirectoryInfo dContainer = new DirectoryInfo(_CONTAINER_FOLDER);
				
				while (dTarget.Parent != null && !isContained) {
					if (dTarget.Parent.FullName == dContainer.FullName)
						isContained = true;
					dTarget = dTarget.Parent;
				}

			}
			catch (Exception e) {
				Logger.LogError("Error ensuring path is contained during processing of " + Prefix + " GET request: " + e.Message);
			}


			return isContained;
		}


		private bool ProcessPost(HttpListenerContext context)
		{
			bool answered = false;
			HttpListenerRequest request = context.Request;

			try {
				string relativePath = "";
				if (!CheckTrustedAgent(context, out answered))
					return answered;

				else if (!EnsurePathIsContained(request.RawUrl, out relativePath))
					answered = RespondToRequest(context, HttpStatusCode.NotFound);

				else if (!request.HasEntityBody)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else if (request.Headers.Get("Signature") == null)
					answered = RespondToRequest(context, HttpStatusCode.BadRequest);

				else if (!(request.ContentType ?? "").Contains("images/"))
					answered = RespondToRequest(context, HttpStatusCode.BadRequest); //Bad Request

				else {
					byte [] payload = new byte [2048];
					int bytesRead = 0;
					using (BinaryReader input = new BinaryReader(request.InputStream)) {
						bytesRead = input.Read(payload, 0, payload.Length);

						int finalBytes = bytesRead < 1024 ? bytesRead : 1024;
						byte [] signatureBuffer = new byte[finalBytes];
						Buffer.BlockCopy(payload, 0, signatureBuffer, 0, finalBytes);
						if (!VerifySignature(request.Headers.Get("Signature"), signatureBuffer))
							answered = RespondToRequest(context, HttpStatusCode.Unauthorized);

						else {
							using (FileStream writer = new FileStream(relativePath, FileMode.OpenOrCreate, FileAccess.Write)) {
								do {
									writer.Write(payload, 0, bytesRead);
								}while((bytesRead = input.Read(payload, 0, payload.Length)) > 0);

								writer.Close();
							}
							RespondToRequest(context, HttpStatusCode.OK);
						}


						input.Close();
					}


				}


			}
			catch (Exception e) {
				Logger.LogError("Error processing " + Prefix + " POST request: " + e.Message);
			}

			return answered;
		}
	}
}
