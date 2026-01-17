using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Songify_Slim.Models.Blocklist
{
    public class BlockedSong : IBlacklistItem
    {
        public string Id { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }

        [YamlIgnore]
        public string Key => Id;

        [YamlIgnore]
        public string Display => $"{Artist} - {Title}";
    }
}