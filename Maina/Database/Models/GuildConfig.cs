namespace Maina.Database.Models
{
    /// <summary>
    /// Guild specific configuration
    /// </summary>
    public class GuildConfig : DatabaseItem
    {
		public ulong NumberId {
			get {
				return ulong.Parse(Id.Substring(Id.LastIndexOf("-") +1));
			}
		}

        public string Prefix { get; set; }

		public ulong? NewsChannel { get; set; }
    }
}