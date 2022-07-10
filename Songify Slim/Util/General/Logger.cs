using System;
using System.IO;

namespace Songify_Slim
{
    internal class Logger
    {
        public static readonly string LogDirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Songify.Rocks", "Logs");

        private static readonly string RootPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Songify.Rocks", "Logs");

        private static void CreateLogDirectory()
        {
            if (!Directory.Exists(LogDirectoryPath)) Directory.CreateDirectory(LogDirectoryPath);
        }

        private static string GetLogFilePath(bool debug = false)
        {
            string date = DateTime.Now.ToString("MM-dd-yyyy");
            string fileName = Path.Combine(RootPath, (debug ? "DEBUG-" : "") + date + ".txt");

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
                    DateTime.Now.ToString("HH:mm:ss") + @": " + exception.Message + Environment.NewLine);
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("HH:mm:ss") + @": " + exception.StackTrace + Environment.NewLine);
                File.AppendAllText(logFile,
                    DateTime.Now.ToString("HH:mm:ss") + @": " + exception.Source + Environment.NewLine);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void LogStr(string s)
        {
            // Writes a log file with exceptions in it
            string logFile = GetLogFilePath();
            try
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("HH:mm:ss") + @": " + s + Environment.NewLine);
            }
            catch
            {
                // ignored
            }
        }
    }
}