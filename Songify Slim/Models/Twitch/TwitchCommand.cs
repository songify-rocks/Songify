using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.General;

namespace Songify_Slim.Models
{
    public class TwitchCommand
    {
        // Parameterless constructor initializing defaults.
        public TwitchCommand()
        {
            IsEnabled = true;
            AllowedUserLevels = []; // Initialize with an empty list.
            IsAnnouncement = false;
            AnnouncementColor = Enums.AnnouncementColor.Primary;
        }

        public Enums.CommandType CommandType { get; set; }

        public string Name => CommandType.GetDescription();
        public string Trigger { get; set; }
        public string Response { get; set; }
        public bool IsEnabled { get; set; }
        public List<int> AllowedUserLevels { get; set; }
        public bool IsAnnouncement { get; set; }
        public Enums.AnnouncementColor AnnouncementColor { get; set; }
        // Dictionary for custom properties.
        public Dictionary<string, object> CustomProperties { get; set; }
    }
}