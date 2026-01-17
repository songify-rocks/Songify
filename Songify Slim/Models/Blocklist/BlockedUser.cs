using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Songify_Slim.Models.Blocklist
{
    public class BlockedUser : IBlacklistItem
    {
        public string Id { get; set; }
        public string Username { get; set; }

        [YamlIgnore]
        public string Key => Username.ToLowerInvariant();

        [YamlIgnore]
        public string Display => Username;
    }
}