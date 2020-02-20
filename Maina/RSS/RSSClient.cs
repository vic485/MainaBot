using Maina.Core.Logging;
using Maina.Database;
using Maina.Database.Models;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Xml;

namespace Maina.RSS
{
	public class RSSClient : IDisposable
	{
		private AutoResetEvent _iterationStart = new AutoResetEvent(false);
		private volatile bool _end = false;
		private readonly DatabaseManager _databaseManager;
		


		public event RSSUpdateEventHandler RSSUpdate;

		public event RSSErrorEventHandler RSSError;


		public  RSSClient(DatabaseManager databaseManager) {
			_databaseManager = databaseManager;
		}

		public void Start () {
			Thread thread = new Thread(new ThreadStart(ThreadLoop));
			thread.Start();
		}

		


		

		private void Work (RSSFeed feed) {
			try {
				using (XmlReader reader = XmlReader.Create(feed.Id)) {
					SyndicationFeed sfeed = SyndicationFeed.Load(reader);
					List<SyndicationItem> items = new List<SyndicationItem>(sfeed.Items);
					int lastUpdateIndex = items.Count;
					for (int i = 0; i < items.Count && lastUpdateIndex == items.Count && feed.LastUpdateId != null; i++) {
						if (items[i].Id == feed.LastUpdateId)
							lastUpdateIndex = i;
					}
					for (int i = lastUpdateIndex -1; i >= 0; i--) {
						//Only invoke event if update is younger than 24 hours
						if ((DateTime.Now.ToLocalTime() - items[i].PublishDate.LocalDateTime) <= TimeSpan.FromHours(24)) {
							//If an exception is thrown while publishing we don't mark the update as published
							RSSUpdate?.Invoke(this, new RSSUpdateEventArgs(items[i], feed));
							feed.LastUpdateId = items[i].Id;
							if (_databaseManager.Exists(feed))
								_databaseManager.Save(feed);
						}
					}
						

				}
			}
			catch (Exception e) {
				Logger.LogError("Error processing a RSS feed: " + e.Message);
			}
		}

		

		private void ThreadLoop()
		{
			try {
				while (!_end) {

					_iterationStart.WaitOne(TimeSpan.FromMinutes(1));

					if (!_end) {
						foreach (RSSFeed feed in _databaseManager.GetAll<RSSFeed>("https://mangadex.org/rss/")) {
							Work(feed);
						}
					}

				}
			}
			catch (Exception e) {
				Logger.LogError("Error in RSSFeed ThreadLoop: " + e.Message);
				RSSError?.Invoke(this, new RSSErrorEventArgs(false));

			}
			finally {
				if (!_disposed)
				Dispose();
			}
		}

		

		
		

		private volatile bool _disposed = false;
		public void Dispose()
		{
			_disposed = true;

			_end = true;
			_iterationStart?.Set();
			_iterationStart?.Dispose();
			_iterationStart = null;

		}
	}
}
