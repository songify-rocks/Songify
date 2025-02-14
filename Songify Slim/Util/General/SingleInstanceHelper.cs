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
        private const string PipeName = "SongifyPipe";

        public static void NotifyFirstInstance()
        {
            try
            {
                using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out);
                client.Connect(2000);
                using StreamWriter writer = new(client);
                writer.AutoFlush = true;
                writer.WriteLine("SHOW");
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }
    }
}