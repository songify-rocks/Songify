using Wpf.Ui.Controls;
using Songify_Slim.Models.Responses;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using Songify_Slim.Views;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using TextBlock = System.Windows.Controls.TextBlock;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for PsaControl.xaml
    /// </summary>
    public partial class PsaControl : UserControl
    {
        public Psa Psa;

        private readonly SymbolIcon _readIcon = new()
        {
            Symbol = SymbolRegular.Checkmark24,
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        public PsaControl(Psa psa, bool byPassLimit = false)
        {
            InitializeComponent();
            Psa = psa;
            TbAuthor.Text = Psa.Author ?? "";
            TbDate.Text = Psa.CreatedAtDateTime?.ToString("dd.MM.yyyy HH:mm") ?? "";
            TbSeverity.Text = Psa.Severity ?? "";

            string message = IoManager.InterpretEscapeCharacters(Psa.MessageText);
            SetTextWithHyperlinks(TbMessage, message);

            if (!byPassLimit)
                DisplayMessageWithReadMore(message);

            Color severityColor = Psa.Severity switch
            {
                "Low" => Color.FromRgb(0x2E, 0x7D, 0x32),
                "Medium" => Color.FromRgb(0xEF, 0x6C, 0x00),
                "High" => Color.FromRgb(0xC6, 0x28, 0x28),
                _ => Color.FromRgb(0x75, 0x75, 0x75)
            };
            SolidColorBrush severityBrush = new(severityColor);
            severityBrush.Freeze();

            BorderSeverity.Background = severityBrush;

            // Left accent for high-severity cards
            if (Psa.Severity == "High")
            {
                BorderMotd.BorderBrush = severityBrush;
                BorderMotd.BorderThickness = new Thickness(3, 1, 1, 1);
            }

            ApplyReadState();
        }

        public void ApplyReadState()
        {
            if (Settings.ReadNotificationIds != null && Settings.ReadNotificationIds.Contains(Psa.Id))
                btnRead.Content = _readIcon;
        }

        private static readonly Regex UrlRegex = new Regex(
    @"(?<url>(https?://[^\s<>()]+)|(\bwww\.[^\s<>()]+))",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private void SetTextWithHyperlinks(TextBlock tb, string text)
        {
            tb.Inlines.Clear();

            if (string.IsNullOrEmpty(text))
                return;

            int lastIndex = 0;

            foreach (Match m in UrlRegex.Matches(text))
            {
                // Add text before the link
                if (m.Index > lastIndex)
                    AddRunsWithLineBreaks(tb, text.Substring(lastIndex, m.Index - lastIndex));

                var raw = m.Groups["url"].Value;
                var uriString = raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? raw
                    : "https://" + raw;

                if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                {
                    var link = new Hyperlink(new Run(raw))
                    {
                        NavigateUri = uri,
                        ToolTip = uri.ToString()
                    };

                    // Optional: you can style links here if you want
                    // link.TextDecorations = null;

                    tb.Inlines.Add(link);
                }
                else
                {
                    // Fallback if parsing fails
                    tb.Inlines.Add(new Run(raw));
                }

                lastIndex = m.Index + m.Length;
            }

            // Add remaining text after last link
            if (lastIndex < text.Length)
                AddRunsWithLineBreaks(tb, text.Substring(lastIndex));
        }

        private static void AddRunsWithLineBreaks(TextBlock tb, string chunk)
        {
            // Preserve line breaks in TextBlock
            var parts = chunk.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) tb.Inlines.Add(new LineBreak());
                if (parts[i].Length > 0) tb.Inlines.Add(new Run(parts[i]));
            }
        }

        private void TbMessage_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link:\n{e.Uri}\n\n{ex.Message}", "Songify", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // Attempt to find accent brush resource
                // Check if the brush is found and apply it
                Brush accentBrush = (Brush)TryFindResource("AccentFillColorDefaultBrush") ?? Brushes.DodgerBlue;

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
                SetTextWithHyperlinks(TbMessage, message);
            }
        }

        private void OpenFullMessageWindow()
        {
            PsaManager.ShowPsaDialog(Psa);
        }

        private void BtnRead_OnClick(object sender, RoutedEventArgs e)
        {
            btnRead.Content = _readIcon;
            PsaManager.MarkAsRead(Psa.Id);
        }
    }
}