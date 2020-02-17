using System.Collections.Generic;

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
        public ulong[] SelfRoleMenu { get; set; } = new ulong[2];
        public Dictionary<string, ulong> SelfRoles { get; set; } = new Dictionary<string, ulong>();

		public Dictionary<string, ulong> NewsRoles {get; set; } = new Dictionary<string, ulong>();
		public ulong? AllNewsRole { get; set; }

		public ulong? NewsChannel { get; set; }
		public ulong WelcomeChannel { get; set; }
		public string WelcomeMessage { get; set; }
    }
}