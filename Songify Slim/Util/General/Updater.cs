using MahApps.Metro.Controls.Dialogs;
using Octokit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;

namespace Songify_Slim
{
    internal class Updater
    {
        public static void CheckForUpdates(Version vs)
        {
            // gets the latest release using OctoKit and compares the version strings (1.0.4 < 1.0.5)
            dynamic latest = GetLatestRelease();
            string currentVersion = vs.ToString().Remove(vs.ToString().Length - 1);
            dynamic onlineVersion = latest.TagName.Replace("v", "");

            dynamic result = onlineVersion.CompareTo(currentVersion);
            if (result > 0)
            {
                VersionCheck(latest);
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    foreach (Window window in System.Windows.Application.Current.Windows)
                    {
                        if (window.GetType() != typeof(MainWindow))
                            continue;

                        //(window as MainWindow).icon_Twitch.Foreground = new SolidColorBrush(Colors.Green);
                        (window as MainWindow).LblStatus.Content = "No new update found.";
                    }
                }));
            }
        }

        public static dynamic GetLatestRelease()
        {
            // access github and get the repository releases
            GitHubClient github = new GitHubClient(new ProductHeaderValue("Songify"));
            Task<IReadOnlyList<Release>> releases = github.Repository.Release.GetAll("inzaniity", "songify");
            Release latest = releases.Result[0]; // Result[0] is always the newest release
            return latest;
        }

        public static async void VersionCheck(dynamic latest)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new Action<dynamic>(VersionCheck), new object[] { latest });
                return;
            }

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() != typeof(MainWindow)) continue;
                ((MainWindow)window).NotifyIcon.BalloonTipTitle = @"New Update!";
                ((MainWindow)window).NotifyIcon.BalloonTipText = @"A new update is available for download. It's recommended to update to the latest version.";
                ((MainWindow)window).NotifyIcon.ShowBalloonTip(500);

                // Make the window larger and show a messagebox with update information ( changelog )
                string changelog = latest.Body;
                (window as MainWindow).Width = 588 + 200;
                (window as MainWindow).Height = 247.881 + 200;
                MessageDialogResult msgResult = await (window as MainWindow).ShowMessageAsync("Notification", "There is a new version available. Do you wish to update?\n\nWhats new:\n" + changelog, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    // if the user wants to update, export config and open url in browser
                    System.Diagnostics.Process.Start(latest.HtmlUrl);
                    ConfigHandler.SaveConfig(AppDomain.CurrentDomain.BaseDirectory + "/config.xml");
                }
                (window as MainWindow).Width = 588;
                (window as MainWindow).Height = 247.881;
            }
        }
    }
}