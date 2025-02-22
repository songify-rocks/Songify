using Songify_Slim.Util.Settings;
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
using MahApps.Metro.Controls;
using Markdig.Parsers;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Markdig.Wpf;
using Songify_Slim.Views;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_CommandItem.xaml
    /// </summary>
    public partial class UC_CommandItem
    {

        public bool ShowBottomBorder
        {
            get => (bool)GetValue(ShowBottomBorderProperty);
            set => SetValue(ShowBottomBorderProperty, value);
        }

        public static readonly DependencyProperty ShowBottomBorderProperty =
            DependencyProperty.Register(
                nameof(ShowBottomBorder),
                typeof(bool),
                typeof(UcUserLevelItem),
                new FrameworkPropertyMetadata(false, null, null));

        public Dictionary<Enums.CommandType, string> Map { get; private set; }


        private static readonly List<int> AllUsers = [0, 1, 2, 3, 4, 5, 6];
        private bool _isUpdating = false;

        public TwitchCommand Command { get; }

        public UC_CommandItem(TwitchCommand cmd)
        {
            InitializeComponent();

            Map = new Dictionary<Enums.CommandType, string>
            {
                { Enums.CommandType.SongRequest, Application.Current.TryFindResource("sw_SpotifySR_SRCommand") as string ?? "Fallback SongRequest" },
                { Enums.CommandType.Next, Application.Current.TryFindResource("brw_cmd_next") as string ?? "Fallback Next" },
                { Enums.CommandType.Play, Application.Current.TryFindResource("brw_cmd_play") as string ?? "Fallback Play" },
                { Enums.CommandType.Voteskip, Application.Current.TryFindResource("brw_cmd_skipvote") as string ?? "Fallback Voteskip" },
                { Enums.CommandType.Position, Application.Current.TryFindResource("brw_cmd_pos") as string ?? "Fallback Position" },
                { Enums.CommandType.Song, Application.Current.TryFindResource("brw_cmd_song") as string ?? "Fallback Song" },
                { Enums.CommandType.Skip, Application.Current.TryFindResource("brw_cmd_skip") as string ?? "Fallback Skip" },
                { Enums.CommandType.Remove, Application.Current.TryFindResource("brw_cmd_remove") as string ?? "Fallback Remove" },
                { Enums.CommandType.Songlike, Application.Current.TryFindResource("brw_cmd_songlike") as string ?? "Fallback Songlike" },
                { Enums.CommandType.Volume, Application.Current.TryFindResource("brw_cmd_vol") as string ?? "Fallback Volume" },
                { Enums.CommandType.Queue, Application.Current.TryFindResource("brw_cmd_queue") as string ?? "Fallback Queue" },
                { Enums.CommandType.Commands, Application.Current.TryFindResource("brw_cmd_commands") as string ?? "Fallback Commands" },
                { Enums.CommandType.Pause, Application.Current.TryFindResource("brw_cmd_pause") as string ?? "Fallback Pause" }
            };

            Command = cmd;
            Loaded += UC_CommandItem_Loaded;
        }

        private void UC_CommandItem_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }

        private void UpdateUi()
        {
            _isUpdating = true;
            MenuItemColorPrimary.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Settings.TwitchUserColor)!);
            TbTrigger.Text = Command.Trigger;
            TglEnabled.IsOn = Command.IsEnabled;
            TbResponse.Text = Command.Response;
            TbDescription.Text = Map.TryGetValue(Command.CommandType, out string description) ? description : "No Description";
            Command.AllowedUserLevels ??= [];
            MenuItemAllNone.Header = Command.AllowedUserLevels.Count == 7 ? "None" : "All";
            MenuAnnounce.IsChecked = Command.IsAnnouncement;

            switch (Command.CommandType)
            {
                case Enums.CommandType.SongRequest:
                    break;
                case Enums.CommandType.Next:
                    break;
                case Enums.CommandType.Play:
                    break;
                case Enums.CommandType.Pause:
                    break;
                case Enums.CommandType.Position:
                    break;
                case Enums.CommandType.Queue:
                    break;
                case Enums.CommandType.Remove:
                    break;
                case Enums.CommandType.Skip:
                    break;
                case Enums.CommandType.Voteskip:
                    if (Command.CustomProperties.TryGetValue("SkipCount", out object skipCountObj) &&
                        skipCountObj is int skipCount)
                    {
                        NudSkipVoteCount.Value = skipCount;
                    }
                    else
                    {
                        Command.CustomProperties["SkipCount"] = 5;
                        NudSkipVoteCount.Value = 5;
                    }
                    PnlVoteSkipExtras.Visibility = Visibility.Visible;
                    break;
                case Enums.CommandType.Song:
                    break;
                case Enums.CommandType.Songlike:
                    break;
                case Enums.CommandType.Volume:
                    // Create a second response textbox if CustomProperties contains a "VolumeSet" key.
                    if (Command.CustomProperties.TryGetValue("VolumeSetResponse", out object volSetResponse) &&
                        volSetResponse is string response)
                    {
                        TbVolSetResponse.Text = response;
                    }
                    else
                    {
                        Command.CustomProperties["VolumeSetResponse"] = "Volume set to {vol}%.";
                        TbVolSetResponse.Text = "Volume set to {vol}%.";
                    }

                    PnlVolSet.Visibility = Visibility.Visible;

                    break;
                case Enums.CommandType.Commands:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _isUpdatingMenuColors = true;
            switch (Command.AnnouncementColor)
            {
                case Enums.AnnouncementColor.Blue:
                    ((MenuItem)MenuColors.Items[0]).IsChecked = true;
                    break;
                case Enums.AnnouncementColor.Green:
                    ((MenuItem)MenuColors.Items[1]).IsChecked = true;

                    break;
                case Enums.AnnouncementColor.Orange:
                    ((MenuItem)MenuColors.Items[2]).IsChecked = true;

                    break;
                case Enums.AnnouncementColor.Purple:
                    ((MenuItem)MenuColors.Items[3]).IsChecked = true;

                    break;
                case Enums.AnnouncementColor.Primary:
                    ((MenuItem)MenuColors.Items[4]).IsChecked = true;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _isUpdatingMenuColors = false;


            foreach (MenuItem menuItem in MenuUserlevels.Items)
            {
                int userlevel = Convert.ToInt32(menuItem.Tag);
                menuItem.IsChecked = Command.AllowedUserLevels.Contains(userlevel);
            }

            if (Command.IsAnnouncement)
            {
                if (CmdBorder.BorderBrush is LinearGradientBrush brush)
                {
                    Color colorFrom;
                    Color colorTo;
                    switch (Command.AnnouncementColor)
                    {
                        case Enums.AnnouncementColor.Blue:
                            colorFrom = (Color)ColorConverter.ConvertFromString("#00d6d6")!;
                            colorTo = (Color)ColorConverter.ConvertFromString("#9146ff")!;
                            break;

                        case Enums.AnnouncementColor.Green:
                            colorFrom = (Color)ColorConverter.ConvertFromString("#00db84")!;
                            colorTo = (Color)ColorConverter.ConvertFromString("#57bee6")!;
                            break;

                        case Enums.AnnouncementColor.Orange:
                            colorFrom = (Color)ColorConverter.ConvertFromString("#ffb31a")!;
                            colorTo = (Color)ColorConverter.ConvertFromString("#e0e000")!;
                            break;

                        case Enums.AnnouncementColor.Purple:
                            colorFrom = (Color)ColorConverter.ConvertFromString("#9146ff")!;
                            colorTo = (Color)ColorConverter.ConvertFromString("#ff75e6")!;
                            break;

                        case Enums.AnnouncementColor.Primary:
                            colorFrom = (Color)ColorConverter.ConvertFromString(Settings.TwitchUserColor)!;
                            colorTo = (Color)ColorConverter.ConvertFromString(Settings.TwitchUserColor)!;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // For example, update the gradient stops:
                    brush.GradientStops.Clear();
                    brush.GradientStops.Add(new GradientStop(colorFrom, 0.0));
                    brush.GradientStops.Add(new GradientStop(colorTo, 1.0));

                    // Optionally, update StartPoint and EndPoint if needed:
                    brush.StartPoint = new Point(0, 0);
                    brush.EndPoint = new Point(0, 1);
                }
            }
            else if (CmdBorder.BorderBrush is LinearGradientBrush brush)
            {
                brush.GradientStops.Clear();
            }

            UpdateUserLevelbadges();
            _isUpdating = false;
        }

        private async void TextBoxTrigger_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || !IsLoaded)
                return;

            string newText = ((TextBox)sender).Text;
            if (newText == Command.Trigger)
                return; // No change, so don't update.

            TextBox tb = (TextBox)sender;
            int startLength = tb.Text.Length;

            await Task.Delay(300);
            if (startLength != tb.Text.Length) return;
            Command.Trigger = newText;
            Settings.UpdateCommand(Command);

        }

        private void TglEnabled_OnToggled(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (!IsLoaded)
                return;
            if (Command.IsEnabled == TglEnabled.IsOn)
                return;

            Command.IsEnabled = TglEnabled.IsOn;
            Settings.UpdateCommand(Command);
        }

        private void UpdateUserLevelbadges()
        {
            List<UcUserLevelItem> itemsToRemove = PnlSongrequestUserlevels.Children.OfType<UcUserLevelItem>().ToList();
            foreach (UcUserLevelItem item in itemsToRemove)
            {
                PnlSongrequestUserlevels.Children.Remove(item);
            }

            foreach (int i in Command.AllowedUserLevels.OrderByDescending(n => n).ToList())
            {
                PnlSongrequestUserlevels.Children.Add(new UcUserLevelItem()
                {
                    UserLevel = i,
                    LongName = false
                });
            }
        }

        private void MenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (!IsLoaded)
                return;
            if (sender is not MenuItem menuItem) return;
            int value = Convert.ToInt32(menuItem.Tag);
            if (Command.AllowedUserLevels.Contains(value)) return;
            List<int> list = [.. Command.AllowedUserLevels, value];
            Command.AllowedUserLevels = list;
            Settings.UpdateCommand(Command);
            UpdateUserLevelbadges();
        }

        private void MenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (!IsLoaded)
                return;
            if (sender is not MenuItem menuItem) return;
            int value = Convert.ToInt32(menuItem.Tag);
            if (!Command.AllowedUserLevels.Contains(value)) return;
            List<int> list = [.. Command.AllowedUserLevels];
            list.Remove(value);
            Command.AllowedUserLevels = list;
            Settings.UpdateCommand(Command);
            UpdateUserLevelbadges();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (_isUpdating)
                return;
            if (sender is not MenuItem) return;
            List<int> list = [.. Command.AllowedUserLevels];
            list = list.Count == AllUsers.Count ? [] : AllUsers;
            Command.AllowedUserLevels = list;
            Settings.UpdateCommand(Command);
            UpdateUi();
            UpdateUserLevelbadges();
        }

        private async void TbResponse_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (_isUpdating)
                return;
            if (Command.Response == ((TextBox)sender).Text)
                return;
            TextBox tb = (TextBox)sender;
            int startLength = tb.Text.Length;

            await Task.Delay(300);
            if (startLength != tb.Text.Length) return;
            Command.Response = ((TextBox)sender).Text;
            Settings.UpdateCommand(Command);
        }

        private void MenuAnnounce_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (_isUpdating)
                return;

            if (Command.IsAnnouncement == ((MenuItem)sender).IsChecked)
                return;


            Command.IsAnnouncement = ((MenuItem)sender).IsChecked;

            Settings.UpdateCommand(Command);
            UpdateUi();
        }

        private bool _isUpdatingMenuColors = false;

        private void MenuColor_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (_isUpdatingMenuColors)
                return; // Prevent reentrancy

            _isUpdatingMenuColors = true;
            try
            {
                // Update the command's announcement color.
                Command.AnnouncementColor = (Enums.AnnouncementColor)int.Parse(
                    ((MenuItem)sender).Tag.ToString().Replace("c", ""));

                // Update the IsChecked state of each menu item.
                foreach (object menuColorsItem in MenuColors.Items)
                {
                    if (menuColorsItem is MenuItem item)
                    {
                        // Set the item as checked if its Tag matches the sender's Tag.
                        item.IsChecked = item.Tag.Equals(((MenuItem)sender).Tag);
                    }
                }

                // Update the command settings after the UI has been updated.
                Settings.UpdateCommand(Command);
                UpdateUi();
            }
            finally
            {
                _isUpdatingMenuColors = false;
            }
        }

        private void NudSkipVoteCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (_isUpdating)
                return;
            if (!IsLoaded)
                return;
            double? d = ((NumericUpDown)sender).Value;
            if (d == null) return;
            int value = (int)d;
            if (Command.CustomProperties.TryGetValue("SkipCount", out object skipCountObj) &&
                skipCountObj is int skipCount)
            {
                if (value == skipCount)
                    return;
            }
            Command.CustomProperties["SkipCount"] = value;
            Settings.UpdateCommand(Command);

        }

        private async void TbVolSetResponse_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (!IsLoaded)
                return;
            TextBox tb = (TextBox)sender;
            int startLength = tb.Text.Length;

            await Task.Delay(300);
            if (startLength != tb.Text.Length) return;
            Command.CustomProperties["VolumeSetResponse"] = ((TextBox)sender).Text;
            Settings.UpdateCommand(Command);
        }
    }
}