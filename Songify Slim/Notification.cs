using System;
using System.Linq;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace Songify_Slim
{
    internal class Notification
    {
        // Notification Options
        public static MessageOptions CreateOptions()
        {
            return new MessageOptions
            {
                FontSize = 12, // set notification font size
                ShowCloseButton = true, // set the option to show or hide notification close button
                FreezeOnMouseEnter = true, // set the option to prevent notification dissapear automatically if user move cursor on it
                UnfreezeOnMouseLeave = true
            };
        }

        public static Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
                corner: Corner.BottomCenter,
                offsetX: 10,
                offsetY: -30);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(1));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public static void ShowNotification(string msg, string type)
        {
            // Types: i = Information, s = Success, w = Warning, e = Error
            switch (type)
            {
                case "i":
                    notifier.ShowInformation(msg, CreateOptions());

                    break;

                case "s":
                    notifier.ShowSuccess(msg, CreateOptions());

                    break;

                case "w":
                    notifier.ShowWarning(msg, CreateOptions());

                    break;

                case "e":
                    notifier.ShowError(msg, CreateOptions());

                    break;
            }
        }
    }
}
