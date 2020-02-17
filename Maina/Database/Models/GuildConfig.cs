using System.Collections.Generic;

namespace Maina.Database.Models
{
    /// <summary>
    /// Guild specific configuration
    /// </summary>
    public class GuildConfig : DatabaseItem
    {
        public string Prefix { get; set; }
        public ulong[] SelfRoleMenu { get; set; } = new ulong[2];
        public Dictionary<string, ulong> SelfRoles { get; set; } = new Dictionary<string, ulong>();
    }
}