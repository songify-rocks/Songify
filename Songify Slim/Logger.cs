using System;
using System.IO;
using System.Reflection;

namespace Songify_Slim
{
    class Logger
    {
        public static void LogExc(Exception exception)
        {
            // Writes a log file with exceptions in it
            string logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".log";
            if (!File.Exists(logPath)) CreateLogFile(logPath);
            File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.Message + Environment.NewLine);
            File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.StackTrace + Environment.NewLine);
            File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.Source + Environment.NewLine);
        }

        public static void LogStr(string s)
        {
            // Writes a log file with exceptions in it
            string logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/" + DateTime.Now.ToString("MM-dd-yyyy") + ".log";
            if (!File.Exists(logPath)) File.Create(logPath).Close();
            File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + s + Environment.NewLine);
        }

        public static void CreateLogFile(string path)
        {
            File.Create(path).Close();
        }
    }
}
