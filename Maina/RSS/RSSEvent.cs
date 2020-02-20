using Maina.Database.Models;
using System;
using System.ServiceModel.Syndication;

namespace Maina.RSS
{
	public delegate void RSSUpdateEventHandler (object sender, RSSUpdateEventArgs e);

	public delegate void RSSErrorEventHandler (object sender, RSSErrorEventArgs e);

	

	public class RSSUpdateEventArgs : EventArgs {
		public SyndicationItem Update { get; set; }

		public RSSFeed Feed { get; set; }

		public string MangaName { get; set; }

		public RSSUpdateEventArgs (SyndicationItem update, RSSFeed feed) {
			Update = update;
			Feed = feed;
		}

	}


	public class RSSErrorEventArgs : EventArgs 
	{

		public bool StillAlive { get; set; }

		public RSSErrorEventArgs (bool stillAlive) {
			StillAlive = stillAlive;
		}
	}
}
