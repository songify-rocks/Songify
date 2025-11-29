using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
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
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_TwitchReward.xaml
    /// </summary>
    public partial class UcTwitchReward
    {
        private readonly CustomReward _reward;

        // Place this as a class-level field:
        private CancellationTokenSource _debounceTokenSource;

        public UcTwitchReward(CustomReward reward, bool manageable)
        {
            InitializeComponent();
            _reward = reward;
            TxtRewardname.Text = _reward.Title;
            TxtRewardcost.Text = _reward.Cost.ToString();
            TxtRewardcost.IsEnabled = manageable;
            ImgManageable.Visibility = manageable ? Visibility.Visible : Visibility.Hidden;

            if (_reward.BackgroundColor != null)
            {
                try
                {
                    ImgBorder.Background = _reward is { BackgroundColor: not null } ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(_reward.BackgroundColor)!) : GetRandomSolidColorBrush();
                }
                catch (Exception)
                {
                    ImgBorder.Background = GetRandomSolidColorBrush();
                }
            }

            if (_reward.Image != null)
                ImgReward.Source = new BitmapImage(new Uri(_reward.Image.Url1x));
            //TglRewardActive.IsOn = Settings.TwRewardId.Any(o => o == _reward.Id);
            // if the reward id is in the skip list, set the combobox to skip, if it's in the sr list, set it to sr else set it to 0
            if (Settings.TwRewardSkipId.Any(o => o == _reward.Id))
            {
                CbxAction.SelectedIndex = 2;
            }
            else if (Settings.TwRewardId.Any(o => o == _reward.Id))
            {
                CbxAction.SelectedIndex = 1;
            }
            else
            {
                CbxAction.SelectedIndex = 0;
            }
        }

        private void TglRewardActive_Toggled(object sender, RoutedEventArgs e)
        {
            List<string> tmp = Settings.TwRewardId;
            string rewardId = _reward.Id;
            if (TglRewardActive.IsOn)
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
            SkipSong
        }

        private void CbxAction_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure sender is a ComboBox and _reward is available
            if (sender is not ComboBox comboBox || _reward == null)
            {
                return;
            }

            // Validate the selected index
            if (comboBox.SelectedIndex is < 0 or > 2)
            {
                return;
            }

            // Cast the selected index to our enum for clarity
            RewardAction action = (RewardAction)comboBox.SelectedIndex;
            string rewardId = _reward.Id;

            // Retrieve the reward lists; initialize if null to avoid null-reference issues
            List<string> songRequestRewards = (Settings.TwRewardId ?? []).ToList();
            List<string> skipSongRewards = (Settings.TwRewardSkipId ?? []).ToList();

            // Process the action based on the selected enum value
            switch (action)
            {
                case RewardAction.Remove:
                    songRequestRewards.Remove(rewardId);
                    skipSongRewards.Remove(rewardId);
                    break;

                case RewardAction.SongRequest:
                    skipSongRewards.Remove(rewardId);
                    AddUnique(songRequestRewards, rewardId);
                    break;

                case RewardAction.SkipSong:
                    songRequestRewards.Remove(rewardId);
                    AddUnique(skipSongRewards, rewardId);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update settings (assuming the setters trigger change notifications or persistence)
            Settings.TwRewardId = songRequestRewards;
            Settings.TwRewardSkipId = skipSongRewards;
        }

        private async void TxtRewardcost_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded)
                    return;

                if (sender is not TextBox textBox || _reward == null)
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
                await TwitchHandler.UpdateRewardCost(_reward.Id, cost);
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void TxtRewardcost_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }
}