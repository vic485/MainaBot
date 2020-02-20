using System;

namespace Maina.HTTP.Server
{
	public delegate void HTTPServerEventHandler (object sender, HTTPServerEventArgs e);

	public class HTTPServerEventArgs : EventArgs {
		public bool StillAlive { get; set; }

		public Exception Exception { get; set; }

		public HTTPServerEventArgs (Exception e, bool stillAlive) {
			Exception = e;
			StillAlive = stillAlive;
		}

	}
}
