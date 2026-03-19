using Songify_Slim.Util.General;
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
using System.Windows.Shapes;
using Windows.UI.Xaml.Data;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_ResponseParams.xaml
    /// </summary>
    public partial class Window_ResponseParams
    {
        public Window_ResponseParams()
        {
            InitializeComponent();
        }

        private Dictionary<string, string> _responseParameters;

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        public void LoadItems()
        {
            _responseParameters = new Dictionary<string, string>
            {
                { "{user}", Application.Current.TryFindResource("param_user_description") as string ?? "The user who triggered the command or channel reward" },
                { "{req}", Application.Current.TryFindResource("param_requester_description") as string ?? "The requester of the current song" },
                { "{{ }}", Application.Current.TryFindResource("param_conditional_text_description") as string ?? "The text inside of '{{' and '}}' will only be posted if the current song is a song request" },
                { "{artist} ", Application.Current.TryFindResource("param_artist_description") as string ?? "Artists for the current song or song request" },
                { "{single_artist}", Application.Current.TryFindResource("param_single_artist_description") as string ?? "Main artist for the current song or song request" },
                { "{errormsg}", Application.Current.TryFindResource("param_error_message_description") as string ?? "Error message if an error occurs" },
                { "{maxlength}", Application.Current.TryFindResource("param_max_length_description") as string ?? "Max song length in minutes" },
                { "{maxreq}", Application.Current.TryFindResource("param_max_requests_description") as string ?? "Max requests per user" },
                { "{song}", Application.Current.TryFindResource("param_song_format_description") as string ?? "{Artist} - {Title}" },
                { "{playlist_name} ", Application.Current.TryFindResource("param_playlist_name_description") as string ?? "Name of the playlist" },
                { "{playlist_url}", Application.Current.TryFindResource("param_playlist_url_description") as string ?? "URL of the playlist" },
                { "{songs}{pos} {song}{/songs}", Application.Current.TryFindResource("param_song_list_description") as string ?? "For !pos command only, creates a list of songs that user has in the queue with their position" },
                { "{votes}", Application.Current.TryFindResource("param_votes_description") as string ?? "Number of votes for voteskip votes/total" },
                { "{cd}", Application.Current.TryFindResource("param_cooldown_description") as string ?? "The cooldown of in seconds (for global cd and user cd)" },
                { "{url}", Application.Current.TryFindResource("param_url_description") as string ?? "Spotify song URL" },
                { "{queue}", Application.Current.TryFindResource("param_queue_description") as string ?? "Next 5 songs in the queue" },
                { "{commands}", Application.Current.TryFindResource("param_commands_description") as string ?? "List of all active commands" },
                { "{userlevel}", Application.Current.TryFindResource("param_userlevel_description") as string ?? "The users userlevel (Folower, Subscriber etc)"},
                { "{ttp}", Application.Current.TryFindResource("param_time_to_play_description") as string ?? "The time in mm:ss when the song request will play."},
                { "{reason}", Application.Current.TryFindResource("param_refund_reason_description") as string ?? "The reason why the reward got refunded."},

            };

            PnlParams.Children.Clear();

            foreach (KeyValuePair<string, string> responseParameter in _responseParameters)
            {
                Button btn = new()
                {
                    Content = new TextBlock
                    {
                        Text = responseParameter.Key
                    },
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };

                btn.Click += BtnOnClick;

                Border border = new()
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
                                        Margin = new Thickness(6,0,0,0)
                                    }
                                }
                            },
                            new TextBlock
                            {
                                Text = responseParameter.Value,
                                TextWrapping = TextWrapping.Wrap
                            }
                        }
                    }
                };
                PnlParams.Children.Add(border);
            }

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
