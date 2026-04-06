using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
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

                PnlParams.Children.Add(CreateParamBorder(entry));
            }
        }

        private static bool MatchesFilter(string filter, ResponseParamEntry entry)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            string haystack = $"{entry.Key} {entry.Description} {entry.Keywords}".ToLowerInvariant();
            foreach (string token in filter.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!haystack.Contains(token))
                    return false;
            }

            return true;
        }

        private Border CreateParamBorder(ResponseParamEntry entry)
        {
            Button btn = new()
            {
                Content = new TextBlock
                {
                    Text = entry.Key
                },
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            btn.Click += BtnOnClick;

            return new Border
            {
                BorderThickness = new Thickness(0),
                Background = Application.Current.TryFindResource("MahApps.Brushes.Accent") as Brush,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(5),
                Child = new StackPanel
                {
                    Margin = new Thickness(6),
                    Children =
                    {
                        new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                btn,
                                new TextBlock
                                {
                                    Text = "",
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Margin = new Thickness(6, 0, 0, 0)
                                }
                            }
                        },
                        new TextBlock
                        {
                            Text = entry.Description,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };
        }

        private async void BtnOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            if (button.Content is TextBlock tbx)
                Clipboard.SetDataObject($"{tbx.Text}");

            if (button.Parent is not StackPanel pnl) return;

            foreach (UIElement pnlChild in pnl.Children)
            {
                if (pnlChild is not TextBlock tb) continue;

                tb.Opacity = 0;
                tb.Text = Application.Current.TryFindResource("common_copied") as string ?? "Copied";
                tb.Text += "!";

                // Fade in over 5 steps (each step is 0.2 opacity, 10ms delay each)
                for (int i = 0; i < 5; i++)
                {
                    tb.Opacity += 0.2;
                    await Task.Delay(10);
                }
                tb.Opacity = 1;

                await Task.Delay(2000);

                // Fade out over 5 steps (each step is 0.2 opacity, 10ms delay each)
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
            if (!IsLoaded)
                return;
            if (Owner is not Window_Settings settings) return;
            settings.LocationChanged -= settings.MetroWindow_LocationChanged;
            settings.Left = Left - settings.Width;
            settings.Top = Top;
            settings.LocationChanged += settings.MetroWindow_LocationChanged;
        }
    }
}