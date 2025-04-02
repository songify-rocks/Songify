using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.Songify;
using TwitchLib.Client.Models;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Models
{
    public class TwitchCommandParams
    {
        public int Subtier { get; set; }
        public bool IsAllowed { get; set; }
        public List<int> UserLevel { get; set; }
        public TwitchUser ExistingUser { get; set; }
    }
}