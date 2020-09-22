using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Songify_Slim
{
    class Logger
    {
        public static void LogExc(Exception exception)
        {
            // Writes a log file with exceptions in it
            string logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log";
            if (!File.Exists(logPath)) CreateLogFile(logPath);
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.Message + Environment.NewLine);
                File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.StackTrace + Environment.NewLine);
                File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + exception.Source + Environment.NewLine);
            }
            catch (Exception)
            {

            }
        }

        public static void DebugLog(string msg, [CallerMemberName] string callingMethod = "", [CallerFilePath] string callingFilePath = "", [CallerLineNumber] int callingFileLineNumber = 0)
        {
            string logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/Debug.log";
            if (!File.Exists(logPath)) CreateLogFile(logPath);
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + msg + " " + callingMethod + "()" + "\t Line: " + callingFileLineNumber + Environment.NewLine);
            }
            catch
            {

            }
        }

        public static void LogStr(string s)
        {
            // Writes a log file with exceptions in it
            string logPath;
            logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/log.log";
            if (!File.Exists(logPath)) CreateLogFile(logPath);
            try
            {
                File.AppendAllText(logPath, DateTime.Now.ToString("hh:mm:ss") + ": " + s + Environment.NewLine);
            }
            catch
            {

            }
        }

        public static void CreateLogFile(string path)
        {
            File.Create(path).Close();
        }
    }
}
