using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for Window_CreateCustomReward.xaml
    /// </summary>
    public partial class Window_CreateCustomReward
    {
        public Window_CreateCustomReward()
        {
            InitializeComponent();
        }

        public async void CreateReward(string name, string prompt, int cost)
        {
            try
            {
                CreateCustomRewardsResponse response = await TwitchHandler._twitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(Settings.TwitchChannelId,
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
                        if (response != null)
                            Debug.WriteLine(response);
            }
            catch (Exception)
            {
                LblStatus.Foreground = Brushes.Red;
                LblStatus.Text = Properties.Resources.crw_CreateRewardError;
                return;
            }
            LblStatus.Foreground = Brushes.ForestGreen;
            LblStatus.Text = Properties.Resources.crw_CreateRewardSuccess.Replace("{name}", name);
            Process.Start("https://dashboard.twitch.tv/viewer-rewards/channel-points/rewards");
        }

        private void BtnCreateReward_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(TbRewardName.Text) || string.IsNullOrWhiteSpace(TbRewardPrompt.Text) || NudRewardCost.Value == null)
                return;
            CreateReward(TbRewardName.Text, TbRewardPrompt.Text, (int)NudRewardCost.Value);
        }
    }
}
