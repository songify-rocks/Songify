using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Models.Responses;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for PsaControl.xaml
    /// </summary>
    public partial class PsaControl : UserControl
    {
        public Psa Psa;

        private readonly PackIconMaterial _readIcon = new()
        {
            Kind = PackIconMaterialKind.Check,
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        public PsaControl(Psa psa, bool byPassLimit = false)
        {
            InitializeComponent();
            Psa = psa;
            TbAuthor.Text = Psa.Author;
            TbDate.Text = Psa.CreatedAtDateTime?.ToString("dd.MM.yyyy HH:mm");
            TbSeverity.Text = Psa.Severity;

            TbMessage.Text = IoManager.InterpretEscapeCharacters(Psa.MessageText);
            if (!byPassLimit)
                DisplayMessageWithReadMore(IoManager.InterpretEscapeCharacters(Psa.MessageText));
            // if the message is longer than 200 characters, add a "read more" clickable text that opens the message in a new window

            Brush severitybrush = Psa.Severity switch
            {
                "Low" => Brushes.ForestGreen,
                "Medium" => Brushes.DarkOrange,
                "High" => Brushes.IndianRed,
                _ => throw new ArgumentOutOfRangeException()
            };

            BorderSeverity.BorderBrush = severitybrush;
            BorderSeverity.Background = severitybrush;

            if (Psa.Severity == "High")
            {
                BorderMotd.BorderBrush = severitybrush;
            }

            if (Settings.ReadNotificationIds != null && Settings.ReadNotificationIds.Contains(psa.Id))
            {
                btnRead.Content = _readIcon;
            }
        }

        private void DisplayMessageWithReadMore(string message)
        {
            const int maxLength = 150;

            // Clear existing inlines to avoid duplication
            TbMessage.Inlines.Clear();

            // Check if the message exceeds 200 characters
            if (message.Length > maxLength)
            {
                // Display the first 200 characters followed by "..."
                string truncatedMessage = message.Substring(0, maxLength) + "... ";

                // Add the truncated message to the TextBlock
                TbMessage.Inlines.Add(new Run(truncatedMessage));

                // Attempt to find the MahApps accent brush resource
                // Check if the brush is found and apply it
                Brush accentBrush = (Brush)TryFindResource("MahApps.Brushes.Accent") ?? Brushes.DodgerBlue;

                // Create a "Read More" Hyperlink
                Hyperlink readMoreLink = new(new Run("read more"))
                {
                    Foreground = accentBrush, // Optional: Style to look like a hyperlink
                    TextDecorations = null // Optional: Remove underline if needed
                };

                // Handle the Click event for the Hyperlink
                readMoreLink.Click += (sender, e) => OpenFullMessageWindow();

                // Add the Hyperlink to the TextBlock
                TbMessage.Inlines.Add(readMoreLink);
            }
            else
            {
                // If the message is 200 characters or less, display it all
                TbMessage.Text = message;
            }
        }

        private void OpenFullMessageWindow()
        {
            // Create a new window to display the full message
            WindowUniversalDialog messageWindow = new(Psa, "Notification")
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontSize = 14
            };
            messageWindow.Show();
        }

        private void BtnRead_OnClick(object sender, RoutedEventArgs e)
        {
            btnRead.Content = _readIcon;
            List<int> readNotificationIds = Settings.ReadNotificationIds;
            if (readNotificationIds != null && readNotificationIds.Contains(Psa.Id))
                return;
            readNotificationIds ??= [];
            readNotificationIds.Add(Psa.Id);
            Settings.ReadNotificationIds = readNotificationIds;

            Window mainWin = Application.Current.MainWindow;
            if (mainWin is MainWindow mainWindow)
            {
                mainWindow.SetUnreadBadge();
            }
        }
    }
}