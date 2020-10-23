using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Songify_Slim
{
    class Logger
    {
        private static string rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        private static string logPath = rootPath + "/log.log";
        private static string debugLogPath = rootPath + "/Debug.log";

        public static void LogExc(Exception exception)
        {
            // Writes a log file with exceptions in it
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
            if (!File.Exists(debugLogPath)) CreateLogFile(debugLogPath);
            try
            {
                File.AppendAllText(debugLogPath, DateTime.Now.ToString("hh:mm:ss") + ": " + msg + " " + callingMethod + "()" + "\t Line: " + callingFileLineNumber + Environment.NewLine);
            }
            catch
            {

            }
        }

        public static void LogStr(string s)
        {
            // Writes a log file with exceptions in it
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
