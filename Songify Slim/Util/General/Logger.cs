using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Songify_Slim.Util.General
{
    internal enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    internal enum LogSource
    {
        Core,
        Twitch,
        Api,
        Cover,
        Spotify,
        Debug,
        Other,
        Pear,
        Songrequest
    }

    internal static class Logger
    {
        public static readonly string LogDirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Songify.Rocks", "Logs");

        // Kept for compatibility, even though it’s the same as LogDirectoryPath
        private static readonly string RootPath = LogDirectoryPath;

        private const string FileTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

        // Used by the WPF console for colors (string-based to stay drop-in compatible)
        private static readonly Dictionary<string, Color> ColorMappings = new()
        {
            { "CORE", Colors.Coral },
            { "TWITCH", Colors.MediumPurple },
            { "API", Colors.LightGreen },
            { "COVER", Colors.Yellow },
            { "PEAR", Color.FromRgb(255, 0, 51) },
            { "SONGREQUEST", Color.FromRgb(91, 142, 192) },
            { "SPOTIFY", Color.FromRgb(30, 215, 96) },
            { "DEBUG", Colors.DodgerBlue }
        };

        private static void CreateLogDirectory()
        {
            if (!Directory.Exists(LogDirectoryPath))
                Directory.CreateDirectory(LogDirectoryPath);
        }

        private static string GetLogFilePath(bool debug = false)
        {
            string date = DateTime.Now.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
            string fileName = Path.Combine(RootPath, (debug ? "DEBUG-" : "") + date + ".txt");

            CreateLogDirectory();

            if (File.Exists(fileName))
                return fileName;

            File.Create(fileName).Close();
            File.AppendAllText(fileName, "Songify Log File" + Environment.NewLine);
            File.AppendAllText(fileName, "====================" + Environment.NewLine);
            File.AppendAllText(fileName, "Version: " + GlobalObjects.AppVersion + Environment.NewLine);
            if (App.IsBeta)
                File.AppendAllText(fileName, "!! BETA !!" + Environment.NewLine);
            File.AppendAllText(fileName, "Date: " + date + Environment.NewLine);
            File.AppendAllText(fileName, "====================" + Environment.NewLine);
            return fileName;
        }

        // ------------- PUBLIC API (drop-in compatible) -------------

        /// <summary>
        /// Old API: log a plain message. Defaults to Info level, source inferred from the text.
        /// </summary>
        public static void LogStr(string message)
        {
            if (message == null) return;

            var source = InferSourceFromMessage(message);
            Log(LogLevel.Info, source, message);
        }

        /// <summary>
        /// Old API: log an exception. Defaults to Error level, Core source.
        /// </summary>
        public static void LogExc(Exception exception)
        {
            if (exception == null) return;

            // Keep your old console output semantics:
            AppendConsole(exception.Message);
            AppendConsole(exception.StackTrace);
            AppendConsole(exception.Source);

            // New structured file logging:
            Log(LogLevel.Error, LogSource.Core, exception.Message, exception);
        }

        // ------------- NEW, MORE STRUCTURED API -------------

        public static void Trace(LogSource source, string message) =>
            Log(LogLevel.Trace, source, message);

        public static void Debug(LogSource source, string message) =>
            Log(LogLevel.Debug, source, message);

        public static void Info(LogSource source, string message) =>
            Log(LogLevel.Info, source, message);

        public static void Warning(LogSource source, string message, Exception? exception = null) =>
            Log(LogLevel.Warning, source, message, exception);

        public static void Error(LogSource source, string message, Exception? exception = null) =>
            Log(LogLevel.Error, source, message, exception);

        public static void Fatal(LogSource source, string message, Exception? exception = null) =>
            Log(LogLevel.Fatal, source, message, exception);

        /// <summary>
        /// Central logging method. Writes to file + console.
        /// </summary>
        public static void Log(LogLevel level, LogSource source, string message, Exception? exception = null)
        {
            var entryTimestamp = DateTime.Now;

            // Build a "source/level" prefix that also works with your existing console formatter.
            string sourceText = source.ToString().ToUpperInvariant();
            string levelText = level.ToString().ToUpperInvariant();
            string consolePayload = $"[{sourceText}] [{levelText}] {message}";

            // Console (uses your existing AppendConsole formatting with time + (1) etc.)
            AppendConsole(consolePayload);

            // File
            WriteToFile(entryTimestamp, level, source, message, exception);
        }

        // ------------- FILE LOGGING -------------

        private static void WriteToFile(DateTime timestamp, LogLevel level, LogSource source,
            string message, Exception? exception)
        {
            try
            {
                bool debugFile = level <= LogLevel.Debug; // Trace/Debug go into DEBUG- files
                string logFile = GetLogFilePath(debugFile);

                string ts = timestamp.ToString(FileTimestampFormat, CultureInfo.InvariantCulture);
                string levelText = level.ToString().ToUpperInvariant();
                string sourceText = source.ToString().ToUpperInvariant();

                var lines = new List<string>
                {
                    $"{ts} [{levelText}] [{sourceText}] {message}"
                };

                if (exception != null)
                {
                    lines.Add($"{ts} [EXCEPTION] {exception}");
                }

                File.AppendAllLines(logFile, lines);
            }
            catch
            {
                // Last resort: swallow. If you want, you can Debug.WriteLine here.
            }
        }

        // ------------- CONSOLE (FlowDocument) LOGGING -------------

        private static void AppendConsole(string s)
        {
            if (s == null) return;

            try
            {
                GlobalObjects.ConsoleDocument.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    // Try to merge with last line if identical (your old behavior)
                    if (GlobalObjects.ConsoleDocument.Blocks.Count > 0)
                    {
                        Paragraph lastParagraph = (Paragraph)GlobalObjects.ConsoleDocument.Blocks.LastBlock;
                        if (lastParagraph != null && lastParagraph.Inlines.Count > 0)
                        {
                            Run lastRun = (Run)lastParagraph.Inlines.LastInline;
                            if (lastRun.Text.Contains(s))
                            {
                                if (!int.TryParse(
                                        Regex.Match(lastRun.Text, @"\(([^)]*)\)").Groups[1].Value,
                                        out int tries))
                                {
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

                    // Add new paragraph
                    GlobalObjects.ConsoleDocument.Blocks.Add(new Paragraph
                    {
                        Margin = new Thickness(0),
                        Inlines =
                        {
                            new Run
                            {
                                Text =
                                    $"[{DateTime.Now.ToString(GlobalObjects.TimeFormat, CultureInfo.InvariantCulture)}] | (1) |  {s}",
                                Foreground = new SolidColorBrush(GetForegroundColor(s))
                            }
                        }
                    });

                    // Limit to last 50 lines
                    int lineCount = GlobalObjects.ConsoleDocument.Blocks.OfType<Paragraph>().Count();
                    if (lineCount <= 50) return;

                    int linesToRemove = lineCount - 50;
                    for (int i = 0; i < linesToRemove; i++)
                    {
                        GlobalObjects.ConsoleDocument.Blocks.Remove(
                            GlobalObjects.ConsoleDocument.Blocks.FirstBlock);
                    }
                }));
            }
            catch
            {
                // DON'T call Logger here to avoid recursion.
                // Optionally: System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static Color GetForegroundColor(string s)
        {
            foreach (KeyValuePair<string, Color> mapping in ColorMappings)
            {
                if (s.IndexOf(mapping.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return mapping.Value;
                }
            }

            return Colors.White; // Default color
        }

        // ------------- HELPER: infer source from message for old API -------------

        private static LogSource InferSourceFromMessage(string s)
        {
            if (s.IndexOf("TWITCH", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Twitch;
            if (s.IndexOf("SPOTIFY", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Spotify;
            if (s.IndexOf("API", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Api;
            if (s.IndexOf("COVER", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Cover;
            if (s.IndexOf("DEBUG", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Debug;
            if (s.IndexOf("CORE", StringComparison.OrdinalIgnoreCase) >= 0)
                return LogSource.Core;
            if (s.IndexOf("PEAR", StringComparison.CurrentCulture) >= 0)
                return LogSource.Pear;
            if (s.IndexOf("SONGREQUEST", StringComparison.CurrentCulture) >= 0)
                return LogSource.Songrequest;
            return LogSource.Other;
        }
    }
}