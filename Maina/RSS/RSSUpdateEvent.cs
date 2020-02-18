using Maina.Database.Models;
using System;
using System.ServiceModel.Syndication;

namespace Maina.RSS
{
	public delegate void RSSUpdateEventHandler (object sender, RSSUpdateEventArgs e);

	public class RSSUpdateEventArgs : EventArgs {
		public SyndicationItem Update { get; set; }

		public RSSFeed Feed { get; set; }

		public string MangaName { get; set; }

		public RSSUpdateEventArgs (SyndicationItem update, RSSFeed feed) {
			Update = update;
			Feed = feed;
		}

	}
}
