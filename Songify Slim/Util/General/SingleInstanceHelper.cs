using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal class SingleInstanceHelper
    {
        public static void NotifyFirstInstance(string[] args)
        {
            try
            {
                // Forward URL if present; otherwise send a simple SHOW command
                string msg = args.Length > 1 ? args[1] : "SHOW";
                PipeMessenger.SendToExistingInstance(msg);
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Core, "Error in SingleInstanceHelper.", e);
            }
        }
    }
}