using System;
using System.Security;
using Microsoft.Win32;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

/// <summary>Windows Run-key autostart registration (independent of any window).</summary>
internal static class AutostartHelper
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Songify";

    public static void RegisterInStartup(bool isChecked)
    {
        try
        {
            using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (registryKey == null)
                throw new UnauthorizedAccessException("Cannot access registry key. Run as administrator.");

            if (isChecked)
            {
                string appPath = AppPaths.GetExecutablePath();
                if (string.IsNullOrEmpty(appPath))
                    throw new InvalidOperationException("Cannot determine application path.");

                registryKey.SetValue(ValueName, appPath);
            }
            else
            {
                registryKey.DeleteValue(ValueName, false);
            }

            Settings.Autostart = isChecked;
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Administrator privileges required to modify startup settings.");
        }
        catch (SecurityException)
        {
            throw new SecurityException("Security policy prevents registry modification.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to modify startup settings: {ex.Message}", ex);
        }
    }
}
