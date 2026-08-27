using Songify_Slim.Util.General;
using MessageDialogResult = Songify_Slim.Util.General.AppDialogResult;
using MessageDialogStyle = Songify_Slim.Util.General.AppDialogStyle;
using MetroDialogSettings = Songify_Slim.Util.General.AppDialogSettings;
using Microsoft.Win32;
using Songify_Slim.Models;

using Songify_Slim.Util.General;

using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Songify_Slim.Util.Configuration;

//using Songify_Slim.Views.WPF_UI;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static Mutex _mutex;
        public static bool IsBeta = true;
        private const string PipeName = "SongifyPipe";
        private const string FolderName = "Songify.Rocks";

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.Error(LogSource.Core, "Unhandled dispatcher exception occurred", e.Exception);
            }
            catch
            {
                // ignore logging failures during crash
            }

            // Prevent a second fatal path / nested crash while we show UI.
            e.Handled = true;
            ShowCrashPromptAndMaybeRestart(e.Exception);
        }

        private App()
        {
            ConfigHandler.ReadConfig();
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            }
            catch (Exception e)
            {
                Logger.Warning(LogSource.Core,
                    $"Couldn't set language '{Settings.Language}', reverting to English.",
                    e);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            }

            // Set before ShellWindow binds StatusBarVersion.
            try
            {
                if (string.IsNullOrEmpty(GlobalObjects.AppVersion))
                    GlobalObjects.AppVersion = AppPaths.GetFileVersionThreePart() ?? "?";
            }
            catch
            {
                // leave empty; UI shows blank instead of crashing
            }

            if (string.IsNullOrEmpty(Settings.Uuid))
            {
                Settings.Uuid = Guid.NewGuid().ToString();
            }

            Logger.PruneOldLogFiles();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.Dispose();
            GlobalObjects.ApiMetrics.Dispose();
            base.OnExit(e);
        }

        public static void BringAllWindowsToFront()
        {
            // Must run on UI thread
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(BringAllWindowsToFront);
                return;
            }

            foreach (Window window in Application.Current.Windows)
            {
                if (!window.IsVisible)
                    continue;

                // Restore if minimized
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                // Force Z-order bump
                window.Activate();         // gives it input focus if possible
                window.Topmost = true;     // push above others
                window.Topmost = false;    // but don?t *stay* always-on-top
            }
        }

        private static void HandleDeepLink(string rawUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawUrl)) return;

                // Some shells pass the arg quoted:  "songify://import-token?token=..."
                string url = rawUrl.Trim().Trim('"');

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    Logger.Error(LogSource.Core, "DeepLink: invalid URI: " + rawUrl);
                    return;
                }

                if (!uri.Scheme.Equals("songify", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error(LogSource.Core, "DeepLink: wrong scheme: " + uri.Scheme);
                    return;
                }

                // For songify://import-token?token=...
                string action = uri.Host; // "import-token"
                NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

                BringAllWindowsToFront();

                switch (action.ToLowerInvariant())
                {
                    case "import-token":
                        {
                            // Accept "token" (primary) and "t" (alias)
                            string token = query["token"] ?? query["t"];
                            token = HttpUtility.UrlDecode(token);

                            if (string.IsNullOrWhiteSpace(token))
                            {
                                // Optional UX: inform the user
                                Logger.Warning(LogSource.Core, "DeepLink: missing token parameter.");
                                return;
                            }

                            // Optional sanity checks (tune to your format/limits)
                            if (token.Length > 4096)
                            {
                                Logger.Warning(LogSource.Core, "DeepLink: token too long.");
                                return;
                            }

                            // If you expect base64url, you could normalize here (only if needed):
                            // token = token.Replace('-', '+').Replace('_', '/'); // then pad '=' and decode

                            // Hand off to your app logic
                            ImportToken(token);

                            // Bring UI to front (assuming you have this)
                            RestoreWindow();

                            // Optional: UX confirmation
                            // Toast/MessageBox/etc.
                            // MessageBox.Show("Token imported successfully.", "Songify", MessageBoxButton.OK, MessageBoxImage.Information);

                            break;
                        }
                    case "twitch-token":
                        {
                            // Accept "token" (primary) and "t" (alias)
                            string token = query["token"] ?? query["t"];
                            token = HttpUtility.UrlDecode(token);

                            if (string.IsNullOrWhiteSpace(token))
                            {
                                // Optional UX: inform the user
                                Logger.Warning(LogSource.Core, "DeepLink: missing token parameter.");
                                return;
                            }

                            // Optional sanity checks (tune to your format/limits)
                            if (token.Length > 4096)
                            {
                                Logger.Warning(LogSource.Core, "DeepLink: token too long.");
                                return;
                            }

                            // If you expect base64url, you could normalize here (only if needed):
                            // token = token.Replace('-', '+').Replace('_', '/'); // then pad '=' and decode

                            // Hand off to your app logic
                            ImportTwitchToken(token);
                            break;
                        }

                    default:
                        Logger.Error(LogSource.Core, "DeepLink: unknown action: " + action);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Failed to handle deep link.", ex);
                MessageBox.Show("Failed to handle deep link.\n" + ex.Message, "Songify", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async void ImportTwitchToken(string token)
        {
            try
            {
                MessageDialogResult result = await AppShellBridge.Current.ShowMessageAsync(
                    "Notification",
                    "Received Twitch Token. Do you want to use this account as Main or Bot?",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "Main",
                        NegativeButtonText = "Bot"
                    }
                );
                if (result == MessageDialogResult.Affirmative)
                {
                    // Main
                    Settings.TwitchAccessToken = token;
                    await TwitchHandler.InitializeApi(Enums.TwitchAccount.Main);
                }
                else
                {
                    // Bot
                    Settings.TwitchBotToken = token;
                    await TwitchHandler.InitializeApi(Enums.TwitchAccount.Bot);
                }

                foreach (Window currentWindow in Current.Windows)
                {
                    if (currentWindow is WindowManualTwitchLogin login)
                        login.Close();
                }

                await SettingsUi.RefreshAsync(resetTwitch: true);
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Core, "Failed to import Twitch Token", e);
            }
        }

        private static async void ImportToken(string token)
        {
            try
            {
                if (AppShellBridge.Current == null)
                {
                    Logger.Warning(LogSource.Core, "DeepLink: cannot confirm Songify API token import ? shell not ready.");
                    return;
                }

                MessageDialogResult confirm = await AppShellBridge.Current.ShowMessageAsync(
                    "Import Songify API Token",
                    "A Songify API token was received via deep link. Do you want to replace your current Songify API token?",
                    MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Import",
                        NegativeButtonText = "Cancel"
                    });

                if (confirm != MessageDialogResult.Affirmative)
                {
                    Logger.Info(LogSource.Core, "DeepLink: Songify API token import cancelled by user.");
                    return;
                }

                Settings.SongifyApiKey = token;
                SongifyAuthService.Invalidate();
                _ = SongifyAuthService.EnsureAuthenticatedAsync();
                _ = SongifyPremiumService.RefreshAsync();

                await AppShellBridge.Current.ShowMessageAsync(
                    "Notification",
                    "Your Songify API Token has been imported successfully",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                    });

                await SettingsUi.RefreshAsync();
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Core, "Failed to import Songify API Token", e);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "Songify";

            CheckOrRegisterDeeplink();

            string[] args = Environment.GetCommandLineArgs();
            // args[0] is exe; args[1] (if present) is the URL like songify://queue?payload=...
            if (args.Length > 1 && args[1].StartsWith("songify://", StringComparison.OrdinalIgnoreCase))
            {
                HandleDeepLink(args[1]);
            }

            // Check if restart argument exists
            bool isRestart = e.Args.Contains("--restart");

            // Mutex logic: bypass if it's a restart
            if (!isRestart)
            {
                _mutex = new Mutex(true, appName, out bool createdNew);
                if (!createdNew)
                {
                    // Mutex exists: app is already running
                    _mutex = Mutex.OpenExisting(appName);
                    if (_mutex != null)
                    {
                        SingleInstanceHelper.NotifyFirstInstance(args);
                        Environment.Exit(0);
                    }
                    Current.Shutdown();
                    return;
                }
            }

            // Register global unhandled exception handler
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            base.OnStartup(e);

            string exePath = AppPaths.GetExecutablePath();

            // Determine the default culture. You can use CultureInfo.CurrentUICulture or a fixed one like "en".
            CultureInfo defaultCulture = CultureInfo.CurrentUICulture;
            // Or for a fixed default, for example:
            // CultureInfo defaultCulture = new CultureInfo("en");

            // Create a localization dictionary from your RESX file.
            ResourceDictionary defaultLocalizationDict = ResxToDictionaryHelper.CreateResourceDictionary(defaultCulture);

            // Add it to the merged dictionaries so that your UI has access to the keys from the start.
            Current.Resources.MergedDictionaries.Add(defaultLocalizationDict);

            UiScaleHandler.Initialize();

            StartPipeServer();
        }

        private static void CheckOrRegisterDeeplink()
        {
            try
            {
                const string scheme = "songify";
                const string baseKey = @"Software\Classes\" + scheme;

                using RegistryKey existing = Registry.CurrentUser.OpenSubKey(baseKey);
                if (existing != null) return; // already registered for this user

                using RegistryKey newKey = Registry.CurrentUser.CreateSubKey(baseKey);
                newKey?.SetValue("", "URL:Songify Protocol", RegistryValueKind.String);
                newKey?.SetValue("URL Protocol", "", RegistryValueKind.String);

                using (RegistryKey defaultIcon = newKey?.CreateSubKey("DefaultIcon"))
                {
                    string iconPath = AppPaths.GetExecutablePath();
                    defaultIcon?.SetValue("", $"\"{iconPath}\",1", RegistryValueKind.String);
                }

                using (RegistryKey commandKey = newKey?.CreateSubKey(@"shell\open\command"))
                {
                    string exePath = AppPaths.GetExecutablePath();
                    commandKey?.SetValue("", $"\"{exePath}\" \"%1\"", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error registering / checking deeplink.", ex);
            }
        }

        public static class ResxToDictionaryHelper
        {
            public static ResourceDictionary CreateResourceDictionary(CultureInfo culture)
            {
                ResourceDictionary dict = new();
                ResourceManager rm = Songify_Slim.Properties.Resources.ResourceManager;
                // Retrieve the resource set for the specified culture.
                ResourceSet resourceSet = rm.GetResourceSet(culture, true, true);
                foreach (DictionaryEntry entry in resourceSet)
                {
                    // Add each key/value pair to the dictionary.
                    dict.Add(entry.Key, entry.Value);
                }
                return dict;
            }
        }

        private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = args.ExceptionObject as Exception;

            try
            {
                if (ex != null)
                {
                    Logger.Fatal(
                        LogSource.Core,
                        $"Unhandled exception caught in MyHandler. IsTerminating={args.IsTerminating}.",
                        ex);

                    if (ex.InnerException != null)
                    {
                        Logger.Error(
                            LogSource.Core,
                            "Unhandled exception has inner exception.",
                            ex.InnerException);
                    }
                }
                else
                {
                    Logger.Fatal(
                        LogSource.Core,
                        $"Unhandled non-Exception object in MyHandler: {args.ExceptionObject} (IsTerminating={args.IsTerminating}).");
                }
            }
            catch
            {
                // ignore logging failures during crash
            }

            if (!args.IsTerminating)
                return;

            ShowCrashPromptAndMaybeRestart(ex);
        }

        /// <summary>
        /// Crash UI must not use WPF MessageBox — with WPF-UI theme dictionaries loaded it can throw
        /// while the app is already failing. WinForms MessageBox is native and more reliable here.
        /// </summary>
        private static void ShowCrashPromptAndMaybeRestart(Exception ex)
        {
            try
            {
                string detail = ex?.GetType().Name ?? "Unknown error";
                if (!string.IsNullOrWhiteSpace(ex?.Message))
                    detail += ": " + ex.Message;

                System.Windows.Forms.DialogResult openLogs = System.Windows.Forms.MessageBox.Show(
                    "Songify ran into a problem and needs to close.\n\n" +
                    detail + "\n\n" +
                    "Would you like to open the log file directory?\n" +
                    "Feel free to submit the log file in our Discord.",
                    "Songify just crashed :(",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Error);

                if (openLogs == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        ShellHelper.OpenPath(Logger.LogDirectoryPath);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                System.Windows.Forms.DialogResult restart = System.Windows.Forms.MessageBox.Show(
                    "Restart Songify?",
                    "Songify",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Question);

                if (restart == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = AppPaths.GetExecutablePath(),
                            Arguments = "--restart",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // Last resort: never let the crash handler itself take down the process messily.
            }

            try
            {
                Current?.Shutdown();
            }
            catch
            {
                try { Environment.Exit(1); } catch { /* ignore */ }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check for the --restart flag
            bool isRestart = e.Args.Contains("--restart");

            // Optionally log or handle restart-specific behavior
            if (isRestart)
            {
                // Perform any specific actions for restarted instance, if needed
                Console.WriteLine("Restarting Songify...");
            }

            // WPF-UI shell (FluentWindow + NavigationView).
            Views.WPFUI.ShellWindow main = new()
            {
                Icon = IsBeta
                    ? new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"))
                    : new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"))
            };

            try
            {
                main.Show();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void StartPipeServer()
        {
            Thread pipeThread = new(() =>
            {
                while (true)
                {
                    try
                    {
                        using NamedPipeServerStream server = new(PipeName, PipeDirection.In);
                        // Wait for a connection (blocking)
                        server.WaitForConnection();

                        using StreamReader reader = new(server, new UTF8Encoding(false));
                        string message = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            message = message.TrimEnd('\r', '\n');

                            if (message == "SHOW")
                            {
                                Current.Dispatcher.Invoke(RestoreWindow);
                            }
                            else if (message.StartsWith("songify://", StringComparison.OrdinalIgnoreCase))
                            {
                                // Handle deep link URL
                                Current.Dispatcher.Invoke(() => HandleDeepLink(message));
                            }
                        }
                    }
                    catch
                    {
                        // Handle exceptions if needed (for example, log them)
                    }
                }
            })
            {
                IsBackground = true
            };

            pipeThread.Start();
        }

        private static void RestoreWindow()
        {
            // Your logic to restore the window from the tray.
            Window win = Current.MainWindow;

            if (win is Views.WPFUI.ShellWindow)
            {
                win.Show();
                win.WindowState = WindowState.Normal;
                Thread.Sleep(1000);
                win.Activate();
            }
        }

        public static void AskDeleteAndRelaunch()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string target = Path.Combine(basePath, FolderName);

            MessageBoxResult result = MessageBox.Show(
                "Songify settings appear corrupted.\n\n" +
                $"Delete the settings folder and restart?\n\n{target}",
                "Reset settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (Directory.Exists(target))
                {
                    foreach (string file in Directory.GetFiles(target, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                    }

                    Directory.Delete(target, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete folder automatically.\nPlease delete it manually:\n\n{target}\n\n{ex.Message}",
                    "Delete failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                try { Process.Start("explorer.exe", basePath); } catch { }
                return;
            }

            // --- Relaunch (Framework-safe) ---
            try
            {
                string exe = AppPaths.GetExecutablePath();
                string args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(QuoteIfNeeded));

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cleanup done, but restarting failed:\n\n" + ex, "Restart Failed");
            }

            Current.Shutdown();
        }

        private static string QuoteIfNeeded(string s)
        {
            return s.Contains(" ") || s.Contains("\"")
                ? "\"" + s.Replace("\"", "\\\"") + "\""
                : s;
        }
    }
}