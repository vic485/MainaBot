using Newtonsoft.Json;

namespace Maina.Database.Models
{
	public class RSSFeed : DatabaseItem
	{

		[JsonIgnore]
		public string LastUpdateId { get; set; }
		public string Tag { get; set; }
	}
}
