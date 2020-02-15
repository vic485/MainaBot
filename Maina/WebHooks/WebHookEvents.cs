using System;

namespace Maina.WebHooks
{
	public delegate void WebHookFailEventHandler (object sender, WebHookFailEventArgs e);
	public delegate void WebHookPayloadEventHandler (object sender, WebHookPayloadEventArgs e);

	public class WebHookFailEventArgs : EventArgs
	{
		public Exception Exception {
			get; set;
		}

		public bool IsServerStillRunning {
			get; set;
		}

		public WebHookFailEventArgs (Exception e, bool stillRunning) {
			Exception = e;
			IsServerStillRunning = stillRunning;
		}

	}

	public class WebHookPayloadEventArgs : EventArgs
	{
		public string Payload {
			get; private set;
		} = null;

		public GitHubWebHookData GitHubReleaseData {
			get; private set;
		}= null;

		public WebHookPayloadEventArgs () {

		}

		public WebHookPayloadEventArgs (string payload) {
			Payload = payload;
		}
		public WebHookPayloadEventArgs (GitHubWebHookData releaseData, string payload = null) {
			GitHubReleaseData = releaseData;
			Payload = payload;
		}


	}

}
