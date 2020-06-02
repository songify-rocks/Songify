using System;
using System.IO;
using System.Reflection;

namespace Songify.Classes
{
    class PathManager
    {
        public static string StartupDirectory
        {
            get => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public string LogDirectory
        {
            get
            {
                string logPath = Path.Combine(StartupDirectory, "Log");
                if (!File.Exists(logPath)) Directory.CreateDirectory(logPath);
                return logPath;
            }
        }

        public string LogFilePath
        {
            get => Path.Combine(LogDirectory, $"{DateTime.Now:dd.MM.yyyy} - Log.txt");
        }

        public string PluginDirectory
        {
            get
            {
                string pluginPath = Path.Combine(StartupDirectory, "Plugins");
                if (!File.Exists(pluginPath)) Directory.CreateDirectory(pluginPath);
                return pluginPath;
            }
        }
    }
}
