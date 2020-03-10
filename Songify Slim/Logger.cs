using System;
using System.IO;
using System.Reflection;

namespace Songify_Slim
{
    class Logger
    {
        public static void Log(Exception exception)
        {
            // Writes a log file with exceptions in it
            string logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".log";
            if (!File.Exists(logPath)) File.Create(logPath).Close();
            File.AppendAllText(logPath, @"----------------- " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + @" -----------------" + Environment.NewLine);
            File.AppendAllText(logPath, exception.Message + Environment.NewLine);
            File.AppendAllText(logPath, exception.StackTrace + Environment.NewLine);
            File.AppendAllText(logPath, exception.Source + Environment.NewLine);
            File.AppendAllText(logPath, Environment.NewLine);

        }
    }
}
