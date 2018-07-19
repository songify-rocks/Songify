using MahApps.Metro.Controls.Dialogs;
using Octokit;
using System;
using System.Windows;

namespace Songify
{
    public class Updater
    {
        public static dynamic getLatestRelease()
        {
            var github = new GitHubClient(new ProductHeaderValue("Songify"));
            var releases = github.Repository.Release.GetAll("inzaniity", "songify");
            var latest = releases.Result[0];
            Console.WriteLine(
                "The latest release is tagged at {0} and is named {1}",
                latest.TagName,
                latest.Name);

            return latest;
        }

        public static void checkForUpdates(Version vs)
        {
            var latest = getLatestRelease();
            var currentVersion = vs.ToString().Remove(vs.ToString().Length - 1);
            var onlineVersion = latest.TagName.Replace("v", "");

            var result = onlineVersion.CompareTo(currentVersion);
            if (result > 0)
            {
                versionCheck(latest);
            }
        }

        public static async void versionCheck(dynamic latest)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new Action<dynamic>(versionCheck), new object[] { latest });
                return;
            }

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    var msgResult = await (window as MainWindow).ShowMessageAsync("Notification", "There is a newer version available. Do you want to update?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (msgResult == MessageDialogResult.Affirmative)
                    {
                        System.Diagnostics.Process.Start(latest.HtmlUrl);
                    }
                }
            }
        }
    }
}