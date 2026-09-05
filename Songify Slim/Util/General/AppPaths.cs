using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Songify_Slim.Util.General;

/// <summary>
/// Path helpers that work for normal builds and PublishSingleFile
/// (where <see cref="Assembly.Location"/> is empty).
/// </summary>
internal static class AppPaths
{
    /// <summary>Directory that contains the running Songify executable.</summary>
    public static string GetAppDirectory()
    {
        string fromProcess = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrWhiteSpace(fromProcess))
            return fromProcess;

        string baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDir))
            return baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string fromAssembly = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        if (!string.IsNullOrWhiteSpace(fromAssembly))
            return fromAssembly;

        return Environment.CurrentDirectory;
    }

    /// <summary>Full path to the running Songify executable.</summary>
    public static string GetExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            return Environment.ProcessPath;

        string location = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(location))
            return location;

        return Path.Combine(GetAppDirectory(), "Songify.exe");
    }

    /// <summary>File version of the running executable (single-file safe).</summary>
    public static string GetFileVersionThreePart()
    {
        try
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(GetExecutablePath());
            if (string.IsNullOrWhiteSpace(fvi.FileVersion))
                return null;

            Version v = new(fvi.FileVersion);
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        catch
        {
            try
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? null : $"{v.Major}.{v.Minor}.{v.Build}";
            }
            catch
            {
                return null;
            }
        }
    }
}
