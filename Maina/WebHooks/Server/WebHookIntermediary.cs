using Maina.Database.Models;
using System;

namespace Maina.WebHooks.Server
{
	public interface WebHookIntermediary
	{
		
		/// <summary>
		/// /// The callback method when a webhook payload is received.
		/// </summary>
		/// <param name="type">The type of the payload.</param>
		/// <param name="payload">The string containing the payload or 
		/// null if there was an error retrieving the payload.</param>
		void OnPayloadReceived (PayloadType type, string payload);


		/// <summary>
		/// The callback method when an exception occurs while listening.
		/// <para>If this call occurs, the WebHook Listener will terminate execution.
		/// Resources are freed after this calls finishes.
		/// Pending requests may still be processed, this is not guaranteed.</para>
		/// </summary>
		/// <param name="e">The exception.</param>
		void OnWebHookListenFail (Exception e, bool stillAlive);

		/// <summary>
		/// The callback method when an exception occurs while processing a request.
		/// <para>If this call occurs, the request may or may not have been answered.
		/// Even if the process fails, OnPayloadReceived will always be called. 
		/// If the process was unable to retrieve the payload, it will be called with a null payload.
		/// </para>
		/// </summary>
		/// <param name="e">The exception.</param>
		void OnWebHookRequestProcessFail (Exception e);


		RSSFeed[] GetRSSFeedList ();

	}
}