


namespace Maina.HTTP.Data
{
	public class GithubRepositoryData {
		public string html_url;
	}
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
		public GithubRepositoryData repository;
	}


}
