using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Songify_Slim.Util.General
{
    public static class RegisterFirewall
    {
        private static readonly string RuleName = "Songify";

        /// <summary>
        /// Inbound TCP rule for the web server. HttpListener uses HTTP.sys, so a
        /// program rule for Songify.exe does not allow LAN clients through.
        /// </summary>
        public const string WebServerRuleName = "Songify WebServer";

        public static void Register()
        {
            if (FirewallRuleExists())
                return;

            if (!IsAdministrator())
            {
                try
                {
                    // Setting up start information for the new process
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C choice /C Y /N /D Y /T 3 & start \"\" \"{AppPaths.GetExecutablePath()}\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    };

                    // Optionally, pass arguments if the elevated instance needs context
                    // startInfo.Arguments = "--someArgument";

                    Process.Start(startInfo);
                    Application.Current.Shutdown();
                }
                catch
                {
                    // Handle the case where the user refused the elevation request
                    // Ask the user to run the application as administrator, and 
                    MessageBox.Show("The application needs to be run as administrator to perform this operation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }
            else
            {
                AddFirewallRule();
            }
        }

        private static void AddFirewallRule()
        {
            string applicationPath = AppPaths.GetExecutablePath();
            ProcessStartInfo startInfo = new("netsh", $"advfirewall firewall add rule name=\"{RuleName}\" dir=in action=allow program=\"{applicationPath}\" enable=yes")
            {
                Verb = "runas", // Request elevation
                CreateNoWindow = true,
                UseShellExecute = true // Required to request elevation
            };

            try
            {
                Process proc = Process.Start(startInfo);
                proc.WaitForExit(); // Wait for the command to complete
            }
            catch (Exception ex)
            {
                // Handle errors (e.g., user refused to grant admin privileges)
                MessageBox.Show($"Failed to add firewall rule. {ex.Message}");
            }
        }

        /// <summary>
        /// Adds or replaces an inbound TCP allow rule for <paramref name="port"/>.
        /// No-op unless this process is elevated.
        /// </summary>
        public static void EnsureWebServerPortRule(int port)
        {
            if (port is < 1025 or > 65535 || !IsAdministrator())
                return;

            try
            {
                // Recreate so a changed port updates the existing rule.
                RunNetsh($"advfirewall firewall delete rule name=\"{WebServerRuleName}\"", out _, out _);
                int exit = RunNetsh(
                    $"advfirewall firewall add rule name=\"{WebServerRuleName}\" dir=in action=allow protocol=TCP localport={port} enable=yes profile=any description=\"Songify HTTP.sys web server\"",
                    out string output, out string error);
                if (exit == 0)
                {
                    Logger.Info(LogSource.Core,
                        $"WebServer: Added inbound firewall rule \"{WebServerRuleName}\" for TCP {port}.");
                    return;
                }

                Logger.Warning(LogSource.Core,
                    $"WebServer: Failed to add firewall rule for TCP {port} (exit {exit}). {(error + " " + output).Trim()}");
            }
            catch (Exception ex)
            {
                Logger.Warning(LogSource.Core, "WebServer: Failed to add firewall rule.", ex);
            }
        }

        /// <summary>
        /// Removes the inbound TCP rule created for the web server. No-op unless elevated.
        /// </summary>
        public static void RemoveWebServerPortRule()
        {
            if (!IsAdministrator())
                return;

            try
            {
                int exit = RunNetsh($"advfirewall firewall delete rule name=\"{WebServerRuleName}\"", out _, out _);
                if (exit == 0)
                    Logger.Info(LogSource.Core,
                        $"WebServer: Removed inbound firewall rule \"{WebServerRuleName}\".");
            }
            catch (Exception ex)
            {
                Logger.Warning(LogSource.Core, "WebServer: Failed to remove firewall rule.", ex);
            }
        }

        private static int RunNetsh(string arguments, out string output, out string error)
        {
            ProcessStartInfo startInfo = new("netsh", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process proc = Process.Start(startInfo);
            if (proc == null)
            {
                output = "";
                error = "Failed to start netsh.";
                return -1;
            }

            output = proc.StandardOutput.ReadToEnd();
            error = proc.StandardError.ReadToEnd();
            proc.WaitForExit(15_000);
            return proc.ExitCode;
        }

        private static bool FirewallRuleExists()
        {

            ProcessStartInfo procStartInfo = new("netsh", "advfirewall firewall show rule name=all")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process proc = new() { StartInfo = procStartInfo };
            proc.Start();

            // Read the output from netsh
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            // Check if our rule name is in the output
            return output.Contains(RuleName);
        }


        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
