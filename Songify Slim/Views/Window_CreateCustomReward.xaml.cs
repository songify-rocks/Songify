using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_CreateCustomReward.xaml
    /// </summary>
    public partial class WindowCreateCustomReward
    {
        public WindowCreateCustomReward()
        {
            InitializeComponent();
        }

        public async Task<CreateCustomRewardsResponse> CreateReward(string name, string prompt, int cost)
        {
            CreateCustomRewardsResponse response;
            try
            {
                response = await TwitchHandler.TwitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(Settings.TwitchChannelId,
                   new CreateCustomRewardsRequest
                   {
                       Title = name,
                       Prompt = prompt,
                       Cost = cost,
                       IsEnabled = true,
                       BackgroundColor = null,
                       IsUserInputRequired = true,
                       IsMaxPerStreamEnabled = false,
                       MaxPerStream = null,
                       IsMaxPerUserPerStreamEnabled = false,
                       MaxPerUserPerStream = null,
                       IsGlobalCooldownEnabled = false,
                       GlobalCooldownSeconds = null,
                       ShouldRedemptionsSkipRequestQueue = false
                   }, Settings.TwitchAccessToken);
            }
            catch (Exception)
            {
                LblStatus.Foreground = Brushes.Red;
                LblStatus.Text = Properties.Resources.crw_CreateRewardError;
                return null;
            }
            LblStatus.Foreground = Brushes.ForestGreen;
            LblStatus.Text = Properties.Resources.crw_CreateRewardSuccess.Replace("{name}", name);
            Process.Start("https://dashboard.twitch.tv/viewer-rewards/channel-points/rewards");
            return response;
        }

        private async void BtnCreateReward_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbRewardName.Text) || string.IsNullOrWhiteSpace(TbRewardPrompt.Text) || NudRewardCost.Value == null)
                return;
            CreateCustomRewardsResponse response = await CreateReward(TbRewardName.Text, TbRewardPrompt.Text, (int)NudRewardCost.Value);
            if (response == null) return;
            foreach (Window window in Application.Current.Windows)
                if (window.GetType() == typeof(Window_Settings))
                {
                    await ((Window_Settings)window).LoadRewards();
                    (window as Window_Settings).CbxRewards.SelectedItem = (window as Window_Settings).GetItemFromList((window as Window_Settings).CbxRewards, response.Data[0].Id);
                }
        }
    }
}
