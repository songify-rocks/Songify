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

        private readonly Dictionary<string, string> _responseParameters = new()
        {
            { "{user}", "The user who triggered the command or channel reward" },
            { "{req}", "The requester of the current song"},
            { "{{ }}", "The text inside of '{{' and '}}' will only be posted if the current song is a song request"},
            { "{artist} ", "Artists for the current song or song request" },
            { "{single_artist}", "Main artist for the current song or song request" },
            { "{errormsg}", "Error message if an error occurs" },
            { "{maxlength}", "Max song length in minutes" },
            { "{maxreq}", "Max requests per user" },
            { "{song}", "{Artist} - {Title}" },
            { "{playlist_name} ", "Name of the playlist" },
            { "{playlist_url}", "URL of the playlist" },
            { "{songs}{pos} {song}{/songs}", "For !pos command only, creates a list of songs that user has in the queue with their position" },
            { "{votes}", "Number of votes for voteskip votes/total" },
            { "{cd}", "The cooldown of in seconds (for global cd and user cd)" },
            { "{url}", "Spotify song URL" },
            { "{queue}", "Next 5 songs in the queue" },
            { "{commands}", "List of all active commands" },
        };

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Top = Owner.Top;
            Left = Owner.Left + Owner.Width;
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
                tb.Text = "Copied!";

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
            if (Owner is not Window_Settings settings) return;
            settings.LocationChanged -= settings.MetroWindow_LocationChanged;
            settings.Left = Left - settings.Width;
            settings.Top = Top;
            settings.LocationChanged += settings.MetroWindow_LocationChanged;
        }
    }
}
