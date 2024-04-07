using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace Songify_Slim.Util.General
{
    public static class RegisterFirewall
    {
        private static readonly string ruleName = "Songify";

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
                        Arguments = $"/C choice /C Y /N /D Y /T 3 & start \"\" \"{Assembly.GetExecutingAssembly().Location}\"",
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
            string applicationPath = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo startInfo = new("netsh", $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{applicationPath}\" enable=yes")
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
            return output.Contains(ruleName);
        }


        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
