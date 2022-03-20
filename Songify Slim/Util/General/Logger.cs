using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Songify_Slim
{
    internal class Logger
    {
        private static readonly string logDirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Songify.Rocks", "Logs");

        private static readonly string rootPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Songify.Rocks", "Logs");

        private static void CreateLogDirectory()
        {
            if (!Directory.Exists(logDirectoryPath)) Directory.CreateDirectory(logDirectoryPath);
        }

        private static string GetLogFilePath(bool debug = false)
        {
            string date = DateTime.Now.ToString("MM-dd-yyyy");
            string fileName = Path.Combine(rootPath, (debug ? "DEBUG-" : "") + date + ".txt");

            CreateLogDirectory();

            if (!File.Exists(fileName)) File.Create(fileName);

            return fileName;
        }

        public static void LogExc(Exception exception)
        {
            // Writes a log file with exceptions in it
            CreateLogDirectory();
            string logFile = GetLogFilePath();
            try
            {
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("HH:mm:ss") + ": " + exception.Message + Environment.NewLine);
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("HH:mm:ss") + ": " + exception.StackTrace + Environment.NewLine);
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("HH:mm:ss") + ": " + exception.Source + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }

        public static void DebugLog(string msg, [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "", [CallerLineNumber] int callingFileLineNumber = 0)
        {
            string logFile = GetLogFilePath(true);
            try
            {
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("hh:mm:ss") + ": " + msg + " " + callingMethod + "()" + "\t Line: " +
                    callingFileLineNumber + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void LogStr(string s)
        {
            // Writes a log file with exceptions in it
            string logFile = GetLogFilePath();
            try
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("hh:mm:ss") + ": " + s + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}