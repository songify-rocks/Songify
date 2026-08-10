using System.Collections.Generic;
using System.Diagnostics;

namespace Songify_Slim.Util.General
{
    internal static class Utils
    {
        public static bool IsDefault<T>(T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default);
        }
    }

    /// <summary>
    /// Opens URLs and filesystem paths via the OS shell.
    /// Required on .NET Core+; Process.Start(string) no longer uses the shell by default.
    /// </summary>
    internal static class ShellHelper
    {
        public static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static void OpenPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
