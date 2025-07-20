using Songify_Slim.Util.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebSockets;
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
using MahApps.Metro.Controls.Dialogs;
using Markdig.Parsers;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Markdig.Wpf;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;
using Songify_Slim.Views;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

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
                { Enums.CommandType.Pause, Application.Current.TryFindResource("brw_cmd_pause") as string ?? "Fallback Pause" },
                { Enums.CommandType.BanSong, Application.Current.TryFindResource("brw_cmd_bansong") as string ?? "Fallback BanSong" }
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
            MenuItemColorPrimary.BorderBrush = string.IsNullOrEmpty(Settings.TwitchUserColor) ? new SolidColorBrush(Colors.Coral) : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Settings.TwitchUserColor)!);
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
                case Enums.CommandType.Next:
                case Enums.CommandType.Play:
                case Enums.CommandType.Pause:
                case Enums.CommandType.Position:
                case Enums.CommandType.Queue:
                case Enums.CommandType.Remove:
                case Enums.CommandType.Skip:
                case Enums.CommandType.Song:
                case Enums.CommandType.Songlike:
                case Enums.CommandType.Commands:
                case Enums.CommandType.BanSong:
                    break;
                case Enums.CommandType.Voteskip:
                    if (Command.CustomProperties.TryGetValue("SkipCount", out object skipCountObj) &&
                        int.TryParse(skipCountObj?.ToString(), out int skipCount))
                    {
                        NudSkipVoteCount.Value = skipCount;
                    }
                    else
                    {
                        skipCount = 5;
                        Command.CustomProperties["SkipCount"] = skipCount; // store as int
                        NudSkipVoteCount.Value = skipCount;
                    }
                    PnlVoteSkipExtras.Visibility = Visibility.Visible;
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

        public void UpdateUserLevelbadges()
        {
            // Step 1: Prepare MenuItems
            List<MenuItem> menuItems = MenuUserlevels.Items.OfType<MenuItem>().ToList();

            // Step 2: Temporarily remove event handlers
            foreach (MenuItem item in menuItems)
            {
                item.Checked -= MenuItemChecked;
                item.Unchecked -= MenuItemUnchecked;
                item.IsChecked = false;
            }

            // Step 3: Clear existing UserLevelItems
            List<UcUserLevelItem> itemsToRemove = PnlSongrequestUserlevels.Children.OfType<UcUserLevelItem>().ToList();
            foreach (UcUserLevelItem item in itemsToRemove)
            {
                PnlSongrequestUserlevels.Children.Remove(item);
            }

            // Step 4: Add allowed user levels
            foreach (int level in Command.AllowedUserLevels.OrderByDescending(n => n))
            {
                PnlSongrequestUserlevels.Children.Add(new UcUserLevelItem()
                {
                    UserLevel = level,
                    LongName = Settings.LongBadgeNames
                });

                // Set corresponding MenuItem checked
                MenuItem matchingMenuItem = menuItems.FirstOrDefault(m => m.Tag?.ToString() == level.ToString());
                if (matchingMenuItem != null)
                {
                    matchingMenuItem.IsChecked = true;
                }
            }

            // Step 5: Add specific allowed users
            foreach (User user in Command.AllowedUsers)
            {
                PnlSongrequestUserlevels.Children.Add(new UcUserLevelItem()
                {
                    UserName = user.DisplayName,
                    UserLevel = -2,
                    LongName = true,
                    UserId = user.Id
                });
            }

            // Step 6: Reattach event handlers
            foreach (MenuItem item in menuItems)
            {
                item.Checked += MenuItemChecked;
                item.Unchecked += MenuItemUnchecked;
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

        private async void MenuExplicitUser_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;
            if (_isUpdating)
                return;
            MetroWindow window = (MetroWindow)Window.GetWindow(this);
            if (window != null)
            {
                string result = await window.ShowInputAsync($"Explicit user for !{Command.Trigger}", "Enter the usernames (comma separated)");

                if (result == null) return;

                List<string> usersToAdd = result.Split(',')
                    .Select(user => user.Trim())
                    .Where(user => !string.IsNullOrEmpty(user))
                    .ToList();

                User[] users = await TwitchApiHelper.GetTwitchUsersAsync(usersToAdd);

                if (users is { Length: > 0 })
                {
                    HashSet<string> existingUserIds = Command.AllowedUsers.Select(u => u.Id).ToHashSet();

                    List<User> newUsers = users
                        .Where(u => !existingUserIds.Contains(u.Id))
                        .ToList();

                    if (newUsers.Count > 0)
                    {
                        Command.AllowedUsers.AddRange(newUsers);
                        Settings.UpdateCommand(Command);
                        UpdateUserLevelbadges();
                    }
                    else
                    {
                        await window.ShowMessageAsync("Info", "All selected users are already added.");
                    }
                }
                else
                {
                    await window.ShowMessageAsync("Error", "No users found. Please check the usernames and try again.");
                }

            }
            else
            {
                // Handle error - window not found
            }
        }
    }
}