using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Songify_Slim.Util.Configuration;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace Songify_Slim.UserControls
{
    /// <summary>Lightweight list row for virtualized reward binding.</summary>
    public sealed class TwitchRewardListItem
    {
        public TwitchRewardListItem(CustomReward reward, bool manageable)
        {
            Reward = reward;
            Id = reward.Id;
            Title = reward.Title;
            Cost = reward.Cost;
            BackgroundColor = reward.BackgroundColor;
            ImageUrl = reward.Image?.Url1x ?? reward.DefaultImage?.Url1x;
            Manageable = manageable;
        }

        private TwitchRewardListItem()
        {
        }

        internal static TwitchRewardListItem FromPowerUp(TwitchPowerUp powerUp)
        {
            return new TwitchRewardListItem
            {
                Id = powerUp.Id,
                Title = powerUp.Title,
                Cost = powerUp.Bits,
                BackgroundColor = powerUp.BackgroundColor,
                ImageUrl = powerUp.ImageUrl,
                IsPowerUp = true,
                Manageable = false
            };
        }

        public CustomReward Reward { get; }
        public string Id { get; private init; }
        public string Title { get; private init; }
        public int Cost { get; private init; }
        public string BackgroundColor { get; private init; }
        public string ImageUrl { get; private init; }
        public bool Manageable { get; private init; }
        public bool IsPowerUp { get; private init; }

        /// <summary>Resource key shown above the first row of a group. Empty for the other rows.</summary>
        public string GroupHeaderKey { get; set; }
    }

    /// <summary>
    /// Interaction logic for UC_TwitchReward.xaml
    /// </summary>
    public partial class UcTwitchReward
    {
        private CustomReward _reward;
        private string _rewardId;
        private bool _isPowerUp;
        private bool _isApplying;

        // Place this as a class-level field:
        private CancellationTokenSource _debounceTokenSource;

        public UcTwitchReward()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => TryBindFromDataContext();
        }

        public UcTwitchReward(CustomReward reward, bool manageable) : this()
        {
            Apply(new TwitchRewardListItem(reward, manageable));
        }

        private void TryBindFromDataContext()
        {
            if (DataContext is TwitchRewardListItem item)
                Apply(item);
        }

        private void Apply(TwitchRewardListItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                return;

            _isApplying = true;
            try
            {
                _reward = item.IsPowerUp ? null : item.Reward;
                _rewardId = item.Id;
                _isPowerUp = item.IsPowerUp;
                ItemSongRequest.Visibility = item.IsPowerUp ? Visibility.Collapsed : Visibility.Visible;
                ItemSongRequest.IsEnabled = !item.IsPowerUp;
                BtnPowerUpNoSongRequest.Visibility = item.IsPowerUp ? Visibility.Visible : Visibility.Collapsed;
                if (item.IsPowerUp)
                    TwitchPowerUpClient.DropSongRequestAssignments([item.Id]);
                TxtRewardname.Text = item.Title;
                TxtRewardcost.Text = item.Cost.ToString();
                TxtRewardcost.IsEnabled = item.Manageable && !item.IsPowerUp;
                ImgCost.Source = new BitmapImage(new Uri(
                    item.IsPowerUp ? "/Resources/img/100.png" : "/Resources/img/default-1.png",
                    UriKind.Relative));
                if (string.IsNullOrEmpty(item.GroupHeaderKey))
                {
                    TxtGroupHeader.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TxtGroupHeader.SetResourceReference(TextBlock.TextProperty, item.GroupHeaderKey);
                    TxtGroupHeader.Visibility = Visibility.Visible;
                }
                ImgManageable.Visibility = item.Manageable ? Visibility.Visible : Visibility.Hidden;
                ToolTip = item.IsPowerUp
                    ? Application.Current?.TryFindResource("uc_reward_powerup_tooltip") as string
                    : null;

                if (!string.IsNullOrWhiteSpace(item.BackgroundColor))
                {
                    try
                    {
                        ImgBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.BackgroundColor)!);
                    }
                    catch (Exception)
                    {
                        ImgBorder.Background = GetRandomSolidColorBrush();
                    }
                }

                if (!string.IsNullOrEmpty(item.ImageUrl) &&
                    Uri.TryCreate(item.ImageUrl, UriKind.Absolute, out Uri imageUri))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = imageUri;
                        bitmap.DecodePixelWidth = 40;
                        bitmap.EndInit();
                        ImgReward.Source = bitmap;
                    }
                    catch
                    {
                        ImgReward.Source = new BitmapImage(new Uri("/Resources/img/default-1.png", UriKind.Relative));
                    }
                }
                else
                {
                    ImgReward.Source = new BitmapImage(new Uri("/Resources/img/default-1.png", UriKind.Relative));
                }

                if (Settings.TwRewardSkipId.Any(o => o == item.Id))
                    CbxAction.SelectedIndex = 2;
                else if (Settings.TwRewardId.Any(o => o == item.Id))
                    CbxAction.SelectedIndex = 1;
                else if (Settings.TwRewardSkipPoll.Any(o => o == item.Id))
                    CbxAction.SelectedIndex = 3;
                else
                    CbxAction.SelectedIndex = 0;
            }
            finally
            {
                _isApplying = false;
            }
        }

        private void TglRewardActive_Toggled(object sender, RoutedEventArgs e)
        {
            List<string> tmp = Settings.TwRewardId;
            string rewardId = _rewardId;
            if (TglRewardActive.IsChecked == true)
            {
                // Only add if it's not already in the list
                if (!tmp.Contains(rewardId))
                {
                    tmp.Add(rewardId);
                }
            }
            else
            {
                tmp.Remove(rewardId);
            }

            Settings.TwRewardId = Settings.TwRewardId;
        }

        public static SolidColorBrush GetRandomSolidColorBrush()
        {
            Random random = new();
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private void AddUnique(List<string> list, string item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        private enum RewardAction
        {
            Remove = 0,
            SongRequest,
            SkipSong,
            StartSkipPoll
        }

        private void CbxAction_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure sender is a ComboBox and _reward is available
            if (_isApplying || sender is not ComboBox comboBox || string.IsNullOrWhiteSpace(_rewardId))
            {
                return;
            }

            // Validate the selected index
            if (comboBox.SelectedIndex is < 0 or > 3)
            {
                return;
            }

            // Cast the selected index to our enum for clarity
            RewardAction action = (RewardAction)comboBox.SelectedIndex;
            if (action == RewardAction.SongRequest && _isPowerUp)
                action = RewardAction.Remove;
            string rewardId = _rewardId;

            // Retrieve the reward lists; initialize if null to avoid null-reference issues
            List<string> songRequestRewards = (Settings.TwRewardId ?? []).ToList();
            List<string> skipSongRewards = (Settings.TwRewardSkipId ?? []).ToList();
            List<string> skipPollRewards = (Settings.TwRewardSkipPoll ?? []).ToList();
            // Process the action based on the selected enum value
            switch (action)
            {
                case RewardAction.Remove:
                    skipPollRewards.Remove(rewardId);
                    songRequestRewards.Remove(rewardId);
                    skipSongRewards.Remove(rewardId);
                    break;

                case RewardAction.SongRequest:
                    skipPollRewards.Remove(rewardId);
                    skipSongRewards.Remove(rewardId);
                    AddUnique(songRequestRewards, rewardId);
                    break;

                case RewardAction.SkipSong:
                    skipPollRewards.Remove(rewardId);
                    songRequestRewards.Remove(rewardId);
                    AddUnique(skipSongRewards, rewardId);
                    break;

                case RewardAction.StartSkipPoll:
                    songRequestRewards.Remove(rewardId);
                    skipSongRewards.Remove(rewardId);
                    AddUnique(skipPollRewards, rewardId);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update settings (assuming the setters trigger change notifications or persistence)
            Settings.TwRewardId = songRequestRewards;
            Settings.TwRewardSkipId = skipSongRewards;
            Settings.TwRewardSkipPoll = skipPollRewards;
        }

        private async void TxtRewardcost_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded)
                    return;

                if (sender is not TextBox textBox || _reward == null || string.IsNullOrWhiteSpace(_rewardId))
                    return;

                // Cancel any previous debounce task
                _debounceTokenSource?.Cancel();
                _debounceTokenSource = new CancellationTokenSource();
                CancellationToken token = _debounceTokenSource.Token;

                // Validate input, but don't update yet
                if (!int.TryParse(textBox.Text, out int cost) || cost <= 0)
                {
                    textBox.Text = MathUtils.Clamp(cost, 1, int.MaxValue).ToString();
                    return;
                }

                // Wait for 500ms of inactivity
                try
                {
                    await Task.Delay(500, token); // throws if cancelled
                }
                catch (TaskCanceledException)
                {
                    return; // A new keystroke happened; abort this update
                }

                // Only update if not cancelled
                await TwitchHandler.UpdateRewardCost(_rewardId, cost);
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch, "Error setting reward cost.", ex);
            }
        }

        private void TxtRewardcost_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnPowerUpNoSongRequest_OnClick(object sender, RoutedEventArgs e)
        {
            ShellHelper.OpenUrl("https://legal.twitch.com/legal/bits-acceptable-use/");
        }
    }
}