using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Songify_Slim.Models.Blocklist
{
    public class BlockedArtist : IBlacklistItem
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [YamlIgnore]
        public string Key => !string.IsNullOrEmpty(Id) ? Id : Name.ToLowerInvariant();

        [YamlIgnore]
        public string Display => Name;
    }
}