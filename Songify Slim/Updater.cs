using MahApps.Metro.Controls.Dialogs;
using Octokit;
using System;
using System.Windows;

namespace Songify_Slim
{
    internal class Updater
    {
        public static dynamic GetLatestRelease()
        {
                var github = new GitHubClient(new ProductHeaderValue("Songify"));
                var releases = github.Repository.Release.GetAll("inzaniity", "songify");
                var latest = releases.Result[0];
                return latest;
        }

        public static void CheckForUpdates(Version vs)
        {
            var latest = GetLatestRelease();
            var currentVersion = vs.ToString().Remove(vs.ToString().Length - 1);
            var onlineVersion = latest.TagName.Replace("v", "");

            var result = onlineVersion.CompareTo(currentVersion);
            if (result > 0)
            {
                VersionCheck(latest);
            }
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
                ((MainWindow)window).NotifyIcon.BalloonTipText = @"A new update is available for download.\nIt's recommended to update to the latest version.";
                ((MainWindow)window).NotifyIcon.ShowBalloonTip(500);

                var msgResult = await (window as MainWindow).ShowMessageAsync("Notification", "There is a newer version available. Do you want to update?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (msgResult == MessageDialogResult.Affirmative)
                {
                    System.Diagnostics.Process.Start(latest.HtmlUrl);
                }
            }
        }
    }
}