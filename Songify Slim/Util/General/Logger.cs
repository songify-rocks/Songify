using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CefSharp.DevTools.CSS;
using Songify_Slim.Util.General;

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
            AppendConsole(exception.Message);
            AppendConsole(exception.StackTrace);
            AppendConsole(exception.Source);


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

        private static void AppendConsole(string s)
        {
            GlobalObjects.ConsoleDocument.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (GlobalObjects.ConsoleDocument.Blocks.Count > 0)
                {
                    Paragraph lastParagraph = (Paragraph)GlobalObjects.ConsoleDocument.Blocks.LastBlock;
                    if (lastParagraph.Inlines.Count > 0)
                    {
                        Run lastRun = (Run)lastParagraph.Inlines.LastInline;
                        if (lastRun.Text.Contains(s))
                        {
                            if (!int.TryParse(Regex.Match(lastRun.Text, @"\(([^)]*)\)").Groups[1].Value, out int tries))
                            {
                                tries = 1;
                                return;
                            }

                            tries++;
                            string str = Regex.Replace(lastRun.Text, @"\([^)]*\)", $"({tries})");
                            str = Regex.Replace(str, @"\[[^)]*\]",
                                $"[{DateTime.Now.ToString(GlobalObjects.TimeFormat, CultureInfo.InvariantCulture)}]");
                            lastRun.Text = str;
                            return;
                        }
                    }
                }

                GlobalObjects.ConsoleDocument.Blocks.Add(new Paragraph
                {
                    Margin = new Thickness(0),
                    Inlines = { new Run
                    {
                        Text = $"[{DateTime.Now.ToString(GlobalObjects.TimeFormat, CultureInfo.InvariantCulture)}] | (1) |  {s}",
                        Foreground = new SolidColorBrush(GetBackgroundColor(s))
                    } }
                });
            }));
        }

        private static Color GetBackgroundColor(string s)
        {
            if (s.Contains("CORE"))
                return Colors.Coral;
            if (s.Contains("TWITCH"))
                return Colors.MediumPurple;
            if (s.Contains("WEB"))
                return Colors.LightGreen;
            if (s.Contains("COVER"))
                return Colors.Yellow;
            return Colors.Transparent;
        }

        public static void LogStr(string s)
        {
            AppendConsole(s);

            // Writes a log file with exceptions in it
            string logFile = GetLogFilePath();
            try
            {
                File.AppendAllText(logFile, DateTime.Now.ToString("HH:mm:ss") + @": " + s + Environment.NewLine);
                //Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + @": " + s);

            }
            catch
            {
                // ignored
            }
        }
    }
}