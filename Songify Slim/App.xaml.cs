using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
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
            Logger.LogExc(e.Exception);
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
                Logger.LogExc(e);
                Logger.LogStr("SYSTEM: Couldn't set language, reverting to english");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            }

            if (string.IsNullOrEmpty(Settings.Uuid))
            {
                Settings.Uuid = Guid.NewGuid().ToString();
            }
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
                window.Topmost = false;    // but don’t *stay* always-on-top
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
                    Logger.LogStr("DeepLink: invalid URI: " + rawUrl);
                    return;
                }

                if (!uri.Scheme.Equals("songify", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogStr("DeepLink: wrong scheme: " + uri.Scheme);
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
                                Logger.LogStr("DeepLink: missing token parameter.");
                                return;
                            }

                            // Optional sanity checks (tune to your format/limits)
                            if (token.Length > 4096)
                            {
                                Logger.LogStr("DeepLink: token too long.");
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
                                Logger.LogStr("DeepLink: missing token parameter.");
                                return;
                            }

                            // Optional sanity checks (tune to your format/limits)
                            if (token.Length > 4096)
                            {
                                Logger.LogStr("DeepLink: token too long.");
                                return;
                            }

                            // If you expect base64url, you could normalize here (only if needed):
                            // token = token.Replace('-', '+').Replace('_', '/'); // then pad '=' and decode

                            // Hand off to your app logic
                            ImportTwitchToken(token);
                            break;
                        }

                    default:
                        Logger.LogStr("DeepLink: unknown action: " + action);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
                MessageBox.Show("Failed to handle deep link.\n" + ex.Message, "Songify", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async void ImportTwitchToken(string token)
        {
            try
            {
                MessageDialogResult result = await ((MainWindow)Current.MainWindow).ShowMessageAsync(
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
                    {
                        login.Close();
                    }

                }

                foreach (Window currentWindow in Current.Windows)
                {
                    if (currentWindow is Window_Settings settings)
                    {
                        await settings.SetControls();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
            }
        }

        private static async void ImportToken(string token)
        {
            Settings.SongifyApiKey = token;
            if ((MainWindow)Current.MainWindow != null)
            {
                MessageDialogResult result = await ((MainWindow)Current.MainWindow).ShowMessageAsync(
               "Notification",
               "Your Songify API Token has been imported successfully",
               MessageDialogStyle.Affirmative, new MetroDialogSettings()
               {
                   AffirmativeButtonText = "OK",
               }
            );
            }

            foreach (Window currentWindow in Current.Windows)
            {
                if (currentWindow is not Window_Settings settings)
                    continue;
                await settings.SetControls();
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

            string exePath = Assembly.GetEntryAssembly()?.Location;

            //AddFirewallException(appName, exePath);

            // Override the Markdig CodeStyleKey at runtime
            if (Current.Resources.Contains(Markdig.Wpf.Styles.CodeStyleKey))
            {
                Style newStyle = new Style(typeof(Run));

                newStyle.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Colors.White)));
                newStyle.Setters.Add(new Setter(TextElement.BackgroundProperty, new SolidColorBrush(Colors.Black)));
                newStyle.Setters.Add(new Setter(TextElement.FontFamilyProperty, new FontFamily("Consolas")));
                newStyle.Setters.Add(new Setter(TextElement.FontSizeProperty, 14.0));

                // Override the existing Markdig Code Style
                Current.Resources[Markdig.Wpf.Styles.CodeStyleKey] = newStyle;
            }

            // Determine the default culture. You can use CultureInfo.CurrentUICulture or a fixed one like "en".
            CultureInfo defaultCulture = CultureInfo.CurrentUICulture;
            // Or for a fixed default, for example:
            // CultureInfo defaultCulture = new CultureInfo("en");

            // Create a localization dictionary from your RESX file.
            ResourceDictionary defaultLocalizationDict = ResxToDictionaryHelper.CreateResourceDictionary(defaultCulture);

            // Add it to the merged dictionaries so that your UI has access to the keys from the start.
            Current.Resources.MergedDictionaries.Add(defaultLocalizationDict);

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
                    string iconPath = Assembly.GetExecutingAssembly().Location;
                    defaultIcon?.SetValue("", $"\"{iconPath}\",1", RegistryValueKind.String);
                }

                using (RegistryKey commandKey = newKey?.CreateSubKey(@"shell\open\command"))
                {
                    string exePath = Assembly.GetExecutingAssembly().Location;
                    commandKey?.SetValue("", $"\"{exePath}\" \"%1\"", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static class ResxToDictionaryHelper
        {
            public static ResourceDictionary CreateResourceDictionary(CultureInfo culture)
            {
                ResourceDictionary dict = new ResourceDictionary();
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
            Exception e = (Exception)args.ExceptionObject;
            Logger.LogStr("##### Unhandled Exception #####");
            Logger.LogStr("MyHandler caught : " + e.Message);
            Logger.LogStr("Stack Trace: " + e.StackTrace);

            if (e.InnerException != null)
            {
                Logger.LogStr("Inner Exception: " + e.InnerException.Message);
                Logger.LogStr("Inner Exception Stack Trace: " + e.InnerException.StackTrace);
            }

            Logger.LogStr("Runtime terminating: " + args.IsTerminating);
            Logger.LogStr("###############################");
            Logger.LogExc(e);

            if (!args.IsTerminating) return;
            if (MessageBox.Show("Would you like to open the log file directory?\n\nFeel free to submit the log file in our Discord.", "Songify just crashed :(",
                    MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                Process.Start(Logger.LogDirectoryPath);
            }

            if (MessageBox.Show("Restart Songify?", "Songify", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
                MessageBoxResult.Yes) return;
            // Pass an argument to indicate this is a restart
            ProcessStartInfo startInfo = new()
            {
                FileName = Assembly.GetExecutingAssembly().Location,
                Arguments = "--restart", // Custom argument
                UseShellExecute = false
            };

            // Start the new process
            Process.Start(startInfo);

            // Shutdown the current instance
            Current.Shutdown();
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

            // Initialize and show the main window
            MainWindow main = new()
            {
                Icon = IsBeta
                    ? new BitmapImage(new Uri("pack://application:,,,/Resources/songifyBeta.ico"))
                    : new BitmapImage(new Uri("pack://application:,,,/Resources/songify.ico"))
            };

            try
            {
                main.Show();
            }
            catch (ConfigurationErrorsException)
            {
                AskDeleteAndRelaunch();
                // throw; // only reached if user said "No"
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

        public static void AddFirewallException(string appName, string exePath)
        {
            string args =
                $"advfirewall firewall add rule name=\"{appName}\" dir=in action=allow program=\"{exePath}\" enable=yes";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                Verb = "runas", // <--- This prompts for admin
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex); // or show MessageBox
            }
        }

        private static void RestoreWindow()
        {
            // Your logic to restore the window from the tray.
            Window win = Current.MainWindow;

            if (win is MainWindow)
            {
                // For example:
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
                string exe = Assembly.GetExecutingAssembly().Location;
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