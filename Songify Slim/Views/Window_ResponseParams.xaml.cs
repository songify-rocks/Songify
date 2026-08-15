using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Songify_Slim.Util.General;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_ResponseParams.xaml
    /// </summary>
    public partial class Window_ResponseParams
    {
        private sealed class ResponseParamEntry
        {
            public string Key { get; }
            public string Description { get; }
            public string Keywords { get; }

            public ResponseParamEntry(string key, string description, string keywords)
            {
                Key = key;
                Description = description;
                Keywords = keywords ?? "";
            }
        }

        private List<ResponseParamEntry> _responseParamEntries = new List<ResponseParamEntry>();

        public Window_ResponseParams()
        {
            InitializeComponent();
            ThemeHandler.ApplyTheme();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RefreshParamList();
        }

        public void LoadItems()
        {
            _responseParamEntries =
            [
                new ResponseParamEntry("{user}",
                    Application.Current.TryFindResource("param_user_description") as string ??
                    "The user who triggered the command or channel reward",
                    "user chatter username name mention who command reward trigger"),

                new ResponseParamEntry("{cmd}",
                    Application.Current.TryFindResource("param_cmd_description") as string ??
                    "The chat command token the user sent (first word of the message)",
                    "command trigger token disabled"),

                new ResponseParamEntry("{req}",
                    Application.Current.TryFindResource("param_requester_description") as string ??
                    "The requester of the current song",
                    "requester current song now playing attribution"),

                new ResponseParamEntry("{{ }}",
                    Application.Current.TryFindResource("param_conditional_text_description") as string ??
                    "The text inside of '{{' and '}}' will only be posted if the current song is a song request",
                    "conditional optional braces song request only if"),

                new ResponseParamEntry("{artist} ",
                    Application.Current.TryFindResource("param_artist_description") as string ??
                    "Artists for the current song or song request",
                    "artist artists band musicians performers"),

                new ResponseParamEntry("{single_artist}",
                    Application.Current.TryFindResource("param_single_artist_description") as string ??
                    "Main artist for the current song or song request",
                    "single artist main first primary"),

                new ResponseParamEntry("{errormsg}",
                    Application.Current.TryFindResource("param_error_message_description") as string ??
                    "Error message if an error occurs",
                    "error fail exception message problem"),

                new ResponseParamEntry("{maxlength}",
                    Application.Current.TryFindResource("param_max_length_description") as string ??
                    "Max song length in minutes",
                    "max length duration minutes limit song length"),

                new ResponseParamEntry("{maxreq}",
                    Application.Current.TryFindResource("param_max_requests_description") as string ??
                    "Max requests per user",
                    "max requests limit per user cap queue quota songs"),

                new ResponseParamEntry("{userreq}",
                    Application.Current.TryFindResource("param_user_requests_description") as string ??
                    "This user's current number of songs in the request queue",
                    "user requests count how many queue songs yours"),

                new ResponseParamEntry("{song}",
                    Application.Current.TryFindResource("param_song_format_description") as string ??
                    "{Artist} - {Title}",
                    "song format artist title dash combined"),

                new ResponseParamEntry("{playlist_name} ",
                    Application.Current.TryFindResource("param_playlist_name_description") as string ??
                    "Name of the playlist",
                    "playlist name title collection"),

                new ResponseParamEntry("{playlist_url}",
                    Application.Current.TryFindResource("param_playlist_url_description") as string ??
                    "URL of the playlist",
                    "playlist url link web address"),

                new ResponseParamEntry("{songs}{pos} {song}{/songs}",
                    Application.Current.TryFindResource("param_song_list_description") as string ??
                    "For !pos command only, creates a list of songs that user has in the queue with their position",
                    "pos position list songs queue order !pos"),

                new ResponseParamEntry("{votes}",
                    Application.Current.TryFindResource("param_votes_description") as string ??
                    "Number of votes for voteskip votes/total",
                    "votes voteskip skip poll tally"),

                new ResponseParamEntry("{cd}",
                    Application.Current.TryFindResource("param_cooldown_description") as string ??
                    "The cooldown of in seconds (for global cd and user cd)",
                    "cooldown cd timer wait seconds delay"),

                new ResponseParamEntry("{url}",
                    Application.Current.TryFindResource("param_url_description") as string ?? "Spotify song URL",
                    "url link spotify http uri"),

                new ResponseParamEntry("{queue}",
                    Application.Current.TryFindResource("param_queue_description") as string ??
                    "Next 5 songs in the queue",
                    "queue upcoming next songs list"),

                new ResponseParamEntry("{commands}",
                    Application.Current.TryFindResource("param_commands_description") as string ??
                    "List of all active commands",
                    "commands list help triggers"),

                new ResponseParamEntry("{userlevel}",
                    Application.Current.TryFindResource("param_userlevel_description") as string ??
                    "The users userlevel (Folower, Subscriber etc)",
                    "userlevel role moderator subscriber follower vip broadcaster"),

                new ResponseParamEntry("{ttp}",
                    Application.Current.TryFindResource("param_time_to_play_description") as string ??
                    "The time in mm:ss when the song request will play.",
                    "ttp time play wait estimate eta mm:ss"),

                new ResponseParamEntry("{reason}",
                    Application.Current.TryFindResource("param_refund_reason_description") as string ??
                    "The reason why the reward got refunded.",
                    "reason refund channel points reward cancelled")

            ];

            RefreshParamList();
        }

        private void RefreshParamList()
        {
            string filter = TxtSearch?.Text ?? "";
            PnlParams.Children.Clear();

            foreach (ResponseParamEntry entry in _responseParamEntries)
            {
                if (!MatchesFilter(filter, entry))
                    continue;

                PnlParams.Children.Add(CreateParamCard(entry));
            }
        }

        private static bool MatchesFilter(string filter, ResponseParamEntry entry)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            string haystack = $"{entry.Key} {entry.Description} {entry.Keywords}".ToLowerInvariant();
            foreach (string token in filter.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries))
            {
                if (!haystack.Contains(token))
                    return false;
            }

            return true;
        }

        private UIElement CreateParamCard(ResponseParamEntry entry)
        {
            var keyBlock = new TextBlock
            {
                Text = entry.Key,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = TryBrush("AccentTextFillColorPrimaryBrush") ?? Brushes.DodgerBlue
            };

            var copyBtn = new Button
            {
                Content = keyBlock,
                Appearance = ControlAppearance.Secondary,
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 0)
            };
            copyBtn.Click += BtnOnClick;

            var copiedHint = new TextBlock
            {
                Text = "",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Opacity = 0,
                Foreground = TryBrush("TextFillColorSecondaryBrush") ?? Brushes.Gray
            };

            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6),
                Children = { copyBtn, copiedHint }
            };

            var description = new TextBlock
            {
                Text = entry.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = TryBrush("TextFillColorSecondaryBrush") ?? Brushes.Gray
            };

            var body = new StackPanel
            {
                Children = { header, description }
            };

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(8),
                Background = TryBrush("CardBackgroundFillColorDefaultBrush")
                              ?? TryBrush("ControlFillColorDefaultBrush")
                              ?? Brushes.Transparent,
                BorderBrush = TryBrush("CardStrokeColorDefaultBrush")
                              ?? TryBrush("ControlStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                Child = body
            };
        }

        private static Brush TryBrush(string key) =>
            Application.Current?.TryFindResource(key) as Brush;

        private async void BtnOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button) return;

            string keyText = button.Content switch
            {
                TextBlock tbx => tbx.Text,
                _ => button.Content?.ToString() ?? ""
            };

            if (!string.IsNullOrEmpty(keyText))
                Clipboard.SetDataObject(keyText);

            if (button.Parent is not StackPanel pnl) return;

            foreach (UIElement pnlChild in pnl.Children)
            {
                if (pnlChild is not TextBlock tb) continue;

                tb.Opacity = 0;
                tb.Text = Application.Current.TryFindResource("common_copied") as string ?? "Copied";
                tb.Text += "!";

                for (int i = 0; i < 5; i++)
                {
                    tb.Opacity += 0.2;
                    await Task.Delay(10);
                }
                tb.Opacity = 1;

                await Task.Delay(2000);

                for (int i = 0; i < 5; i++)
                {
                    tb.Opacity -= 0.2;
                    await Task.Delay(10);
                }
                tb.Opacity = 0;
                tb.Text = "";
            }
        }

        public void Window_ResponseParams_OnLocationChanged(object sender, EventArgs e)
        {
            if (!IsLoaded || Owner is not Window owner)
                return;

            owner.Left = Left - owner.ActualWidth;
            owner.Top = Top;
        }
    }
}
