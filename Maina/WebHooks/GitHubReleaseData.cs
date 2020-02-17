


namespace Maina.WebHooks
{
	
	public class GithubReleaseData {
		public string html_url;
		public string tag_name;
		public string name;
		public bool draft;
	}
	public class GitHubWebHookData
	{
		public string action;
		public GithubReleaseData release;
	}


}
