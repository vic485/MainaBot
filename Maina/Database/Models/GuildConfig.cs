using System;
using System.Collections.Generic;
using System.Text;

namespace Maina.Database.Models
{
	public class RoleMenu {
		public ulong? Channel { get; set; }
		public ulong? Message { get; set; }

		public Dictionary<string, ulong> SelfRoles { get; set; } = new Dictionary<string, ulong>();
	}

	public class GuildConfig : DatabaseItem
	{
		public ulong NumberId {
			get {
				return ulong.Parse(Id.Substring(Id.LastIndexOf("-") +1));
			}
		}

        public string Prefix { get; set; }
        public RoleMenu DefaultSelfRoleMenu { 
			get {
				if(!SelfRoleMenus.ContainsKey("default"))
					SelfRoleMenus.Add("default", new RoleMenu());
				return SelfRoleMenus["default"];
			}
		}
		public Dictionary<string, RoleMenu> SelfRoleMenus { get; set; } = new Dictionary<string, RoleMenu>();


		public Dictionary<string, ulong> NewsRoles {get; set; } = new Dictionary<string, ulong>();
		public ulong? AllNewsRole { get; set; }

		public ulong? NewsChannel { get; set; }
		public ulong WelcomeChannel { get; set; }
		public string WelcomeMessage { get; set; }
	}
}
